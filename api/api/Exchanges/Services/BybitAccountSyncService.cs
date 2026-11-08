using api.AzureKeyVault;
using api.Cache;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Models.Cryptos;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Services;

public sealed class BybitAccountSyncService : IBybitAccountSyncService
{
    private static readonly TimeSpan SyncOverlap = TimeSpan.FromMinutes(5);
    // Bybit order and transfer history accept at most 7 days per request (deposits and withdrawals 30).
    private static readonly TimeSpan MaxWindow = TimeSpan.FromDays(7);
    // Lookback when a status has no checkpoint at all (legacy rows without BybitCredentialsSetAt);
    // matches Bybit's own 30-day default for deposit and withdrawal history.
    private static readonly TimeSpan InitialLookback = TimeSpan.FromDays(30);
    // Caps the catch-up work of a single run; a checkpoint further behind resumes on the next run.
    private const int MaxWindowsPerSync = 8;

    private readonly IBybitService _bybitService;
    private readonly IBybitOrderSyncService _orderSyncService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ICacheService _cacheService;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly ILogger<BybitAccountSyncService> _logger;

    public BybitAccountSyncService(
        IBybitService bybitService,
        IBybitOrderSyncService orderSyncService,
        IKeyVaultService keyVaultService,
        DataContext context,
        ICacheService cacheService,
        ICoinMarketCapService coinMarketCapService,
        ILogger<BybitAccountSyncService> logger)
    {
        _bybitService = bybitService;
        _orderSyncService = orderSyncService;
        _keyVaultService = keyVaultService;
        _context = context;
        _cacheService = cacheService;
        _coinMarketCapService = coinMarketCapService;
        _logger = logger;
    }

    public async Task<BybitAccountSyncResult> SyncAsync(Account account, CancellationToken cancellationToken)
    {
        try
        {
            var userId = account.UserId;
            var accountId = account.Id;
            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(status => status.UserId == userId
                    && status.AccountId == accountId
                    && status.ExchangeName == "Bybit", cancellationToken);

            if (syncStatus is null)
            {
                _logger.LogInformation("Bybit sync: no sync status for user {UserId}, account {AccountId} (credentials may predate safeguard), skipping",
                    userId, accountId);
                return BybitAccountSyncResult.Skipped("Bybit sync is not configured for this account");
            }

            if (!syncStatus.IsEnabled)
            {
                _logger.LogInformation("Bybit sync: account {AccountId} is disabled, skipping", accountId);
                return BybitAccountSyncResult.Skipped("Bybit sync is disabled for this account");
            }

            if (!await IsIntegrationEnabledAsync(userId, cancellationToken))
                return BybitAccountSyncResult.Skipped("Bybit integration is disconnected or auto-paused");

            var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, accountId, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, accountId, "api-secret", cancellationToken);

            if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
            {
                const string errorMessage = "Credential storage is temporarily unavailable";
                await _orderSyncService.MarkSyncStatusErrorAsync(userId, accountId, errorMessage, cancellationToken);
                return BybitAccountSyncResult.Failed(errorMessage, 503);
            }

            if (string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
            {
                const string errorMessage = "Bybit credentials are not configured for this account";
                return BybitAccountSyncResult.Skipped(errorMessage);
            }

            var region = BybitEndpoints.Parse(syncStatus.Region);
            await PopulateInitialCashBalanceAsync(account, apiKey.Value, apiSecret.Value, region, cancellationToken);

            // Fetch from the checkpoint to now in explicit windows of at most 7 days. With only a
            // startTime, Bybit returns startTime + 7 days for orders/transfers (silently dropping
            // newer items) and rejects deposits/withdrawals older than 30 days with 131002.
            var now = DateTimeOffset.UtcNow;
            var cutoff = syncStatus.LastSyncAt ?? syncStatus.BybitCredentialsSetAt;
            var windowStart = cutoff is { } dateTime
                ? new DateTimeOffset(dateTime.Subtract(SyncOverlap), TimeSpan.Zero)
                : now - InitialLookback;
            var events = new List<SyncEvent>();
            for (var windowCount = 0; windowStart < now && windowCount < MaxWindowsPerSync; windowCount++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var windowEnd = windowStart + MaxWindow < now ? windowStart + MaxWindow : now;
                events.AddRange(await CollectEventsAsync(
                    apiKey.Value, apiSecret.Value, region, (windowStart, windowEnd), dropOutsideWindow: false, cancellationToken));
                windowStart = windowEnd;
            }

            var checkpointAt = windowStart.UtcDateTime;

            // Process every Bybit event in time order so a buy is applied after the deposit,
            // transfer or sell that funded it. Cash is not mutated per event: Bybit is the source
            // of truth for cash, so it is reconciled to the wallet once all events are recorded.
            var hasFailures = false;
            string? lastOrderId = null;

            foreach (var syncEvent in events.OrderBy(item => item.Timestamp).ThenBy(item => item.Kind))
            {
                if (!await ProcessEventAsync(syncEvent, account, region, apiKey.Value, apiSecret.Value, "RestPoll", cancellationToken))
                    hasFailures = true;

                if (syncEvent.Value is BybitOrderData order)
                    lastOrderId = order.OrderId;
            }

            if (hasFailures)
            {
                const string errorMessage = "One or more Bybit items failed to process; checkpoint was not advanced";
                return BybitAccountSyncResult.Failed(errorMessage);
            }

            await ReconcileCurrentCryptoBalancesAsync(account, apiKey.Value, apiSecret.Value, region, cancellationToken);
            await ReconcileCashBalanceAsync(account, apiKey.Value, apiSecret.Value, region, cancellationToken);

            await _orderSyncService.UpsertSyncStatusAsync(userId, accountId, lastOrderId, cancellationToken, checkpointAt);
            return BybitAccountSyncResult.Succeeded($"Bybit account {account.Name} synchronized successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Bybit sync canceled for account {AccountId}", account.Id);
            throw;
        }
        catch (BybitApiException ex)
        {
            var failure = BybitApiFailureClassifier.Classify(ex);
            var message = $"Bybit rejected sync request: {ex.RetCode} - {ex.RetMsg}";
            await MarkErrorAsync(account, message, cancellationToken);
            if (failure.Kind == BybitApiFailureKind.PermanentCredential)
                await PauseForPermanentCredentialFailureAsync(account, failure, message, cancellationToken);
            return BybitAccountSyncResult.Failed(message, 400, failure.Kind, failure.Code, failure.Endpoint);
        }
        catch (BybitTransportException ex)
        {
            var message = $"Bybit request failed while connecting to {ex.Endpoint}";
            await MarkErrorAsync(account, message, cancellationToken);
            return BybitAccountSyncResult.Failed(message, 503, BybitApiFailureKind.Transport, "transport", ex.Endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bybit sync failed for account {AccountId}", account.Id);
            await MarkErrorAsync(account, ex.Message, cancellationToken);
            return BybitAccountSyncResult.Failed(ex.Message);
        }
    }

    public async Task<BybitAccountSyncResult> BackfillAsync(Account account, DateTime fromUtc, CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc));
        var end = DateTimeOffset.UtcNow;
        if (start >= end)
            return BybitAccountSyncResult.Failed("The import date must be before now.", 400);

        if (start < end.AddYears(-2))
            return BybitAccountSyncResult.Failed("Bybit history imports can cover at most two years.", 400);

        try
        {
            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(status => status.UserId == account.UserId
                    && status.AccountId == account.Id
                    && status.ExchangeName == "Bybit", cancellationToken);
            if (syncStatus is null)
                return BybitAccountSyncResult.Skipped("Bybit sync is not configured for this account");

            if (!syncStatus.IsEnabled)
                return BybitAccountSyncResult.Skipped("Bybit sync is disabled for this account");

            if (!await IsIntegrationEnabledAsync(account.UserId, cancellationToken))
                return BybitAccountSyncResult.Skipped("Bybit integration is disconnected or auto-paused");

            var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, account.UserId, account.Id, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, account.UserId, account.Id, "api-secret", cancellationToken);
            if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
                return BybitAccountSyncResult.Failed(KeyVaultSecretReadResult.UnavailableMessage, 503);

            if (string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
                return BybitAccountSyncResult.Skipped("Bybit credentials are not configured for this account");

            var region = BybitEndpoints.Parse(syncStatus.Region);
            var events = new List<SyncEvent>();
            var windowStart = start;

            while (windowStart < end)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var windowEnd = windowStart.AddDays(7) < end ? windowStart.AddDays(7) : end;
                events.AddRange(await CollectEventsAsync(
                    apiKey.Value, apiSecret.Value, region, (windowStart, windowEnd), dropOutsideWindow: true, cancellationToken));
                windowStart = windowEnd;
            }

            var processed = 0;
            string? lastOrderId = null;
            foreach (var backfillEvent in events.OrderBy(item => item.Timestamp).ThenBy(item => item.Kind))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var succeeded = await ProcessEventAsync(
                    backfillEvent, account, region, apiKey.Value, apiSecret.Value, "BybitBackfill", cancellationToken);

                if (!succeeded)
                    return BybitAccountSyncResult.Failed("One or more historical items failed to process; the import can be retried safely.");

                if (backfillEvent.Value is BybitOrderData order)
                    lastOrderId = order.OrderId;
                processed++;
            }

            await ReconcileCurrentCryptoBalancesAsync(account, apiKey.Value, apiSecret.Value, region, cancellationToken);

            await _orderSyncService.UpsertSyncStatusAsync(
                account.UserId,
                account.Id,
                lastOrderId,
                cancellationToken,
                end.UtcDateTime);
            return BybitAccountSyncResult.Succeeded($"Imported {processed} Bybit historical items without changing the cash balance.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (BybitApiException ex)
        {
            var failure = BybitApiFailureClassifier.Classify(ex);
            var message = $"Bybit rejected the historical import: {ex.RetCode} - {ex.RetMsg}";
            if (failure.Kind == BybitApiFailureKind.PermanentCredential)
                await PauseForPermanentCredentialFailureAsync(account, failure, message, cancellationToken);
            return BybitAccountSyncResult.Failed(
                message,
                400,
                failure.Kind,
                failure.Code,
                failure.Endpoint);
        }
        catch (BybitTransportException ex)
        {
            return BybitAccountSyncResult.Failed(
                $"Bybit request failed while connecting to {ex.Endpoint}",
                503,
                BybitApiFailureKind.Transport,
                "transport",
                ex.Endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bybit historical import failed for account {AccountId}", account.Id);
            return BybitAccountSyncResult.Failed("Bybit historical import failed. Check the sync logs for details.");
        }
    }

    public async Task<BybitAccountSyncResult> RecalculateAsync(Account account, CancellationToken cancellationToken)
    {
        try
        {
            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(status => status.UserId == account.UserId
                    && status.AccountId == account.Id
                    && status.ExchangeName == "Bybit", cancellationToken);
            if (syncStatus is null)
                return BybitAccountSyncResult.Skipped("Bybit sync is not configured for this account");

            if (!syncStatus.IsEnabled)
                return BybitAccountSyncResult.Skipped("Bybit sync is disabled for this account");

            if (!await IsIntegrationEnabledAsync(account.UserId, cancellationToken))
                return BybitAccountSyncResult.Skipped("Bybit integration is disconnected or auto-paused");

            var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, account.UserId, account.Id, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, account.UserId, account.Id, "api-secret", cancellationToken);

            if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
                return BybitAccountSyncResult.Failed("Credential storage is temporarily unavailable", 503);

            if (string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
                return BybitAccountSyncResult.Skipped("Bybit credentials are not configured for this account");

            await ReconcileCurrentCryptoBalancesAsync(
                account,
                apiKey.Value,
                apiSecret.Value,
                BybitEndpoints.Parse(syncStatus.Region),
                cancellationToken);

            return BybitAccountSyncResult.Succeeded("Bybit crypto balances recalculated successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (BybitApiException ex)
        {
            var failure = BybitApiFailureClassifier.Classify(ex);
            var message = $"Bybit rejected balance reconciliation: {ex.RetCode} - {ex.RetMsg}";
            if (failure.Kind == BybitApiFailureKind.PermanentCredential)
                await PauseForPermanentCredentialFailureAsync(account, failure, message, cancellationToken);
            return BybitAccountSyncResult.Failed(
                message,
                400,
                failure.Kind,
                failure.Code,
                failure.Endpoint);
        }
        catch (BybitTransportException ex)
        {
            return BybitAccountSyncResult.Failed(
                $"Bybit request failed while connecting to {ex.Endpoint}",
                503,
                BybitApiFailureKind.Transport,
                "transport",
                ex.Endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bybit balance recalculation failed for account {AccountId}", account.Id);
            return BybitAccountSyncResult.Failed("Bybit balance recalculation failed. Check the sync logs for details.");
        }
    }

    private async Task ReconcileCurrentCryptoBalancesAsync(
        Account account,
        string apiKey,
        string apiSecret,
        BybitRegion region,
        CancellationToken cancellationToken)
    {
        var cryptoAssets = account.CryptoAssets
            .Where(asset => !BybitCashBalance.IsCashCoin(asset.Symbol))
            .ToList();

        foreach (var cashAsset in account.CryptoAssets.Where(asset => BybitCashBalance.IsCashCoin(asset.Symbol)))
        {
            cashAsset.ReconcileBalance(0);
            _cacheService.Remove($"{CacheKeyConstants.UserCryptoAsset}{cashAsset.Id}");
        }

        foreach (var asset in cryptoAssets)
            asset.RecalculateFromTransactions();

        // Bybit's wallet-balance endpoint supports UNIFIED only. FUND balances
        // use a different account-coin endpoint.
        var walletResponse = await _bybitService.GetWalletBalanceAsync(
            apiKey,
            apiSecret,
            region,
            accountType: "UNIFIED",
            account.ExternalId);
        var currentBalances = new Dictionary<string, decimal>(
            BybitCashBalance.SumWalletBalances([walletResponse]),
            StringComparer.OrdinalIgnoreCase);
        var fundBalancesAvailable = false;
        try
        {
            var fundResponse = await _bybitService.GetAccountCoinBalancesAsync(
                apiKey,
                apiSecret,
                region,
                accountType: "FUND",
                account.ExternalId);
            fundBalancesAvailable = true;
            foreach (var coin in fundResponse.Result.Balance)
            {
                if (!string.IsNullOrWhiteSpace(coin.Coin))
                    currentBalances[coin.Coin] = currentBalances.GetValueOrDefault(coin.Coin)
                        + BybitCashBalance.ParseAccountCoinBalance(coin);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            && BybitApiFailureClassifier.Classify(ex).Kind == BybitApiFailureKind.Other)
        {
            _logger.LogDebug(ex,
                "Bybit balance reconciliation could not fetch FUND balances for account {AccountId}",
                account.Id);
        }

        foreach (var balance in currentBalances.Where(balance => balance.Value > 0 && !BybitCashBalance.IsCashCoin(balance.Key)))
        {
            if (account.CryptoAssets.Any(asset => asset.Symbol.Equals(balance.Key, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                var quote = await _coinMarketCapService.GetQuoteBySymbol(balance.Key.ToUpperInvariant());
                if (quote?.Data is null || !quote.Data.Any())
                {
                    _logger.LogWarning("Bybit balance reconciliation could not resolve CoinMarketCap metadata for {Symbol}", balance.Key);
                    continue;
                }

                var coin = quote.Data.First().Value;
                var asset = new CryptoAsset(coin.Name, coin.Name, coin.Symbol, coin.Id);
                if (account.AddCryptoAsset(asset).IsSuccess)
                {
                    cryptoAssets.Add(asset);
                    _logger.LogInformation(
                        "Bybit balance reconciliation created crypto asset {Symbol} (CMC ID {Id})",
                        coin.Symbol,
                        coin.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Bybit balance reconciliation failed to create crypto asset for {Symbol}",
                    balance.Key);
            }
        }

        foreach (var asset in cryptoAssets)
        {
            if (!currentBalances.ContainsKey(asset.Symbol) && !fundBalancesAvailable)
            {
                _logger.LogWarning(
                    "Bybit balance reconciliation skipped {Symbol} for account {AccountId}; current balance is unavailable",
                    asset.Symbol,
                    account.Id);
                continue;
            }

            var currentBalance = currentBalances.GetValueOrDefault(asset.Symbol);
            if (asset.Balance != currentBalance)
            {
                _logger.LogWarning(
                    "Bybit balance reconciliation changed {Symbol} for account {AccountId}: historical {HistoricalBalance}, Bybit {BybitBalance}",
                    asset.Symbol,
                    account.Id,
                    asset.Balance,
                    currentBalance);
            }

            asset.ReconcileBalance(currentBalance);
            _cacheService.Remove($"{CacheKeyConstants.UserCryptoAsset}{asset.Id}");
        }

        await _context.SaveChangesAsync(cancellationToken);
        _cacheService.Remove($"{CacheKeyConstants.UserAccountDetails}{account.UserId}");
        _cacheService.Remove(CacheKeyConstants.GetLastCryptoAssetsCacheKeyForUser(account.UserId.ToString()));
    }

    private async Task<List<SyncEvent>> CollectEventsAsync(
        string apiKey,
        string apiSecret,
        BybitRegion region,
        (DateTimeOffset Start, DateTimeOffset End) window,
        bool dropOutsideWindow,
        CancellationToken cancellationToken)
    {
        var events = new List<SyncEvent>();
        var startTime = window.Start.ToUnixTimeMilliseconds();
        var endTime = window.End.ToUnixTimeMilliseconds();

        // Backfill drops rows without a usable timestamp or outside the window. The incremental sync
        // keeps every row Bybit returned (timestamp-less rows ordered last); downstream processing is
        // idempotent, so a row returned by two adjacent windows is recorded once.
        void Add<T>(IEnumerable<T> rows, SyncEventKind kind, Func<T, DateTimeOffset?> timestampOf) where T : notnull
        {
            foreach (var row in rows)
            {
                var timestamp = timestampOf(row);
                if (dropOutsideWindow && (timestamp is null || timestamp < window.Start || timestamp >= window.End))
                    continue;
                events.Add(new SyncEvent(timestamp ?? DateTimeOffset.MaxValue, kind, row));
            }
        }

        var orders = await _bybitService.GetOrderHistoryAsync(apiKey, apiSecret, region, 50, startTime, cancellationToken, endTime);
        Add(orders.Where(IsExecuted), SyncEventKind.Order,
            order => TryGetTimestamp(order, out var timestamp) ? timestamp : null);

        var deposits = await _bybitService.GetDepositHistoryAsync(apiKey, apiSecret, region, 50, startTime, cancellationToken, endTime);
        Add(deposits, SyncEventKind.Deposit,
            deposit => TryGetTimestamp(deposit.SuccessAt, out var timestamp) ? timestamp : null);

        var withdrawals = await _bybitService.GetWithdrawalHistoryAsync(apiKey, apiSecret, region, 50, startTime, cancellationToken, endTime);
        Add(withdrawals, SyncEventKind.Withdrawal,
            withdrawal => TryGetTimestamp(withdrawal, out var timestamp) ? timestamp : null);

        var transfers = await _bybitService.GetInternalTransferHistoryAsync(apiKey, apiSecret, region, 50, startTime, cancellationToken, endTime);
        Add(transfers, SyncEventKind.InternalTransfer,
            transfer => TryGetTimestamp(transfer.Timestamp, out var timestamp) ? timestamp : null);

        var universalTransfers = await _bybitService.GetUniversalTransferHistoryAsync(apiKey, apiSecret, region, 50, startTime, cancellationToken, endTime);
        Add(universalTransfers, SyncEventKind.UniversalTransfer,
            transfer => TryGetTimestamp(transfer.Timestamp, out var timestamp) ? timestamp : null);

        return events;
    }

    // A spot order that partly filled before being canceled still executed its filled quantity;
    // AvgPrice and CumExecQty describe only the executed part, so it is imported like a fill.
    private static bool IsExecuted(BybitOrderData order) =>
        order.OrderStatus == "Filled"
        || (order.OrderStatus == "PartiallyFilledCanceled"
            && decimal.TryParse(order.CumExecQty, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var executedQty)
            && executedQty > 0);

    // Events are recorded without touching account cash; callers reconcile cash to Bybit afterwards.
    private async Task<bool> ProcessEventAsync(
        SyncEvent syncEvent,
        Account account,
        BybitRegion region,
        string apiKey,
        string apiSecret,
        string importSource,
        CancellationToken cancellationToken) => syncEvent.Kind switch
    {
        SyncEventKind.Order => await ProcessOrderAsync((BybitOrderData)syncEvent.Value, account, region, apiKey, apiSecret, importSource, cancellationToken),
        SyncEventKind.Deposit => await _orderSyncService.ProcessDepositAsync((BybitDepositWithdrawalRow)syncEvent.Value, account, account.UserId, cancellationToken, applyCashBalance: false),
        SyncEventKind.Withdrawal => await _orderSyncService.ProcessWithdrawalAsync((BybitDepositWithdrawalRow)syncEvent.Value, account, account.UserId, cancellationToken, applyCashBalance: false),
        SyncEventKind.InternalTransfer or SyncEventKind.UniversalTransfer
            => await _orderSyncService.ProcessInternalTransferAsync((BybitInternalTransferRow)syncEvent.Value, account, account.UserId, cancellationToken, applyCashBalance: false),
        _ => false
    };

    private async Task<bool> ProcessOrderAsync(
        BybitOrderData order,
        Account account,
        BybitRegion region,
        string apiKey,
        string apiSecret,
        string importSource,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<BybitExecutionData> executions = [];
        try
        {
            executions = await _bybitService.GetExecutionHistoryAsync(apiKey, apiSecret, region, order.OrderId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            && BybitApiFailureClassifier.Classify(ex).Kind == BybitApiFailureKind.Other)
        {
            _logger.LogDebug(ex, "Bybit execution history unavailable for order {OrderId}; using order fee details", order.OrderId);
        }

        return await _orderSyncService.ProcessOrderAsync(
            order,
            account,
            account.UserId,
            importSource,
            cancellationToken,
            executions,
            applyCashBalance: false);
    }

    private static bool TryGetTimestamp(BybitOrderData order, out DateTimeOffset timestamp)
    {
        timestamp = default;
        return long.TryParse(order.CreatedTime, out var milliseconds)
            && TryGetTimestamp(milliseconds, out timestamp);
    }

    private static bool TryGetTimestamp(string? value, out DateTimeOffset timestamp)
    {
        if (long.TryParse(value, out var milliseconds))
            return TryGetTimestamp(milliseconds, out timestamp);

        return DateTimeOffset.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }

    private static bool TryGetTimestamp(BybitDepositWithdrawalRow withdrawal, out DateTimeOffset timestamp)
        => TryGetTimestamp(withdrawal.CreateTime ?? withdrawal.SuccessAt, out timestamp);

    private static bool TryGetTimestamp(long milliseconds, out DateTimeOffset timestamp)
    {
        timestamp = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        return true;
    }

    private enum SyncEventKind
    {
        Order,
        Deposit,
        Withdrawal,
        InternalTransfer,
        UniversalTransfer
    }

    private sealed record SyncEvent(DateTimeOffset Timestamp, SyncEventKind Kind, object Value);

    private async Task PopulateInitialCashBalanceAsync(
        Account account,
        string apiKey,
        string apiSecret,
        BybitRegion region,
        CancellationToken cancellationToken)
    {
        try
        {
            var cash = await GetBybitCashBalanceAsync(account, apiKey, apiSecret, region);
            await _orderSyncService.ProcessOpeningBalanceAsync(account, account.UserId, cash.Total, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            && BybitApiFailureClassifier.Classify(ex).Kind == BybitApiFailureKind.Other)
        {
            _logger.LogDebug(ex, "Bybit sync: failed to fetch initial cash balance for account {AccountId}", account.Id);
        }
    }

    // Bybit already executed every synced order, so its wallet is the source of truth for cash.
    // Only a complete read replaces the local balance; a partial or failed read leaves it unchanged.
    private async Task ReconcileCashBalanceAsync(
        Account account,
        string apiKey,
        string apiSecret,
        BybitRegion region,
        CancellationToken cancellationToken)
    {
        (decimal Total, bool IsComplete) cash;
        try
        {
            cash = await GetBybitCashBalanceAsync(account, apiKey, apiSecret, region);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Bybit cash reconciliation skipped for account {AccountId}: wallet balance unavailable", account.Id);
            return;
        }

        if (!cash.IsComplete)
        {
            _logger.LogWarning("Bybit cash reconciliation skipped for account {AccountId}: wallet balance read was incomplete", account.Id);
            return;
        }

        if (account.Balance == cash.Total)
            return;

        _logger.LogWarning(
            "Bybit cash reconciliation changed account {AccountId} cash: local {LocalBalance}, Bybit {BybitBalance}",
            account.Id,
            account.Balance,
            cash.Total);
        account.AddToBalance(cash.Total - account.Balance);
        await _context.SaveChangesAsync(cancellationToken);
        _cacheService.Remove($"{CacheKeyConstants.UserAccountDetails}{account.UserId}");
    }

    private async Task<(decimal Total, bool IsComplete)> GetBybitCashBalanceAsync(
        Account account,
        string apiKey,
        string apiSecret,
        BybitRegion region)
    {
        var total = 0m;
        var isComplete = true;
        foreach (var accountType in new[] { "FUND", "UNIFIED" })
        {
            foreach (var coin in BybitCashBalance.CashCoins)
            {
                try
                {
                    var coinBalance = await _bybitService.GetAccountCoinBalanceAsync(
                        apiKey,
                        apiSecret,
                        region,
                        accountType,
                        coin,
                        account.ExternalId);
                    total += BybitCashBalance.FromAccountCoinBalance(coinBalance);
                }
                catch (Exception ex) when (ex is not OperationCanceledException
                    && BybitApiFailureClassifier.Classify(ex).Kind == BybitApiFailureKind.Other)
                {
                    isComplete = false;
                    _logger.LogDebug(ex, "Bybit sync: failed to fetch {AccountType} {Coin} balance for account {AccountId}",
                        accountType, coin, account.Id);
                }
            }
        }

        return (total, isComplete);
    }

    private async Task MarkErrorAsync(Account account, string message, CancellationToken cancellationToken)
    {
        try
        {
            await _orderSyncService.MarkSyncStatusErrorAsync(account.UserId, account.Id, message, cancellationToken);
        }
        catch (Exception error)
        {
            _logger.LogError(error, "Bybit sync: failed to persist error status for account {AccountId}", account.Id);
        }
    }

    private async Task PauseForPermanentCredentialFailureAsync(
        Account account,
        BybitApiFailure failure,
        string message,
        CancellationToken cancellationToken)
    {
        var integration = await _context.ExchangeIntegrations
            .SingleOrDefaultAsync(item => item.UserId == account.UserId && item.Exchange == "Bybit", cancellationToken);
        if (integration is null || !integration.Enabled)
            return;

        if (!integration.RecordPermanentCredentialFailure(
                failure.Code ?? "credential-rejected",
                message,
                failure.Endpoint ?? "Bybit API request",
                account.Id,
                DateTime.UtcNow))
            return;

        await _context.SaveChangesAsync(cancellationToken);
        BybitCredentialReader.Invalidate(_keyVaultService, account.UserId, null);
        var accountIds = await _context.Accounts
            .Where(candidate => candidate.UserId == account.UserId
                && !candidate.IsDeleted
                && candidate.AccountType == EAccountType.Exchange
                && candidate.Exchange == "Bybit")
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken);
        foreach (var accountId in accountIds)
            BybitCredentialReader.Invalidate(_keyVaultService, account.UserId, accountId);

        _logger.LogError(
            "Bybit integration automatically paused after permanent credential error for user {UserId}, account {AccountId}, error code {ErrorCode}, endpoint {Endpoint}",
            account.UserId,
            account.Id,
            integration.LastErrorCode,
            integration.LastErrorEndpoint);
    }

    private async Task<bool> IsIntegrationEnabledAsync(int userId, CancellationToken cancellationToken)
    {
        var enabled = await _context.ExchangeIntegrations
            .Where(integration => integration.UserId == userId && integration.Exchange == "Bybit")
            .Select(integration => (bool?)integration.Enabled)
            .SingleOrDefaultAsync(cancellationToken);
        return enabled ?? true;
    }
}
