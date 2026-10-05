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

            var cutoff = syncStatus.LastSyncAt ?? syncStatus.BybitCredentialsSetAt;
            var startTime = cutoff is { } dateTime
                ? new DateTimeOffset(dateTime.Subtract(SyncOverlap), TimeSpan.Zero).ToUnixTimeMilliseconds()
                : (long?)null;
            var orders = await _bybitService.GetOrderHistoryAsync(
                apiKey.Value,
                apiSecret.Value,
                region,
                limit: 50,
                startTime: startTime,
                cancellationToken: cancellationToken);
            var hasFailures = false;

            foreach (var order in orders.Where(order => order.OrderStatus == "Filled"))
            {
                IReadOnlyList<BybitExecutionData> executions = [];
                try
                {
                    executions = await _bybitService.GetExecutionHistoryAsync(
                        apiKey.Value,
                        apiSecret.Value,
                        region,
                        order.OrderId,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Bybit sync: execution history unavailable for order {OrderId}; using order fee details", order.OrderId);
                }

                if (!await _orderSyncService.ProcessOrderAsync(
                    order,
                    account,
                    userId,
                    "RestPoll",
                    cancellationToken,
                    executions))
                {
                    hasFailures = true;
                }
            }

            var deposits = await _bybitService.GetDepositHistoryAsync(
                apiKey.Value,
                apiSecret.Value,
                region,
                limit: 50,
                startTime: startTime,
                cancellationToken: cancellationToken);
            foreach (var deposit in deposits)
            {
                if (!await _orderSyncService.ProcessDepositAsync(deposit, account, userId, cancellationToken))
                    hasFailures = true;
            }

            var withdrawals = await _bybitService.GetWithdrawalHistoryAsync(
                apiKey.Value,
                apiSecret.Value,
                region,
                limit: 50,
                startTime: startTime,
                cancellationToken: cancellationToken);
            foreach (var withdrawal in withdrawals)
            {
                if (!await _orderSyncService.ProcessWithdrawalAsync(withdrawal, account, userId, cancellationToken))
                    hasFailures = true;
            }

            var internalTransfers = await _bybitService.GetInternalTransferHistoryAsync(
                apiKey.Value,
                apiSecret.Value,
                region,
                limit: 50,
                startTime: startTime,
                cancellationToken: cancellationToken);
            foreach (var internalTransfer in internalTransfers)
            {
                if (!await _orderSyncService.ProcessInternalTransferAsync(internalTransfer, account, userId, cancellationToken))
                    hasFailures = true;
            }

            if (hasFailures)
            {
                const string errorMessage = "One or more Bybit items failed to process; checkpoint was not advanced";
                return BybitAccountSyncResult.Failed(errorMessage);
            }

            await ReconcileCurrentCryptoBalancesAsync(account, apiKey.Value, apiSecret.Value, region, cancellationToken);

            var lastOrderId = orders.Count > 0 ? orders.Last().OrderId : null;
            await _orderSyncService.UpsertSyncStatusAsync(userId, accountId, lastOrderId, cancellationToken);
            return BybitAccountSyncResult.Succeeded($"Bybit account {account.Name} synchronized successfully");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Bybit sync canceled for account {AccountId}", account.Id);
            throw;
        }
        catch (BybitApiException ex)
        {
            var message = $"Bybit rejected sync request: {ex.RetCode} - {ex.RetMsg}";
            await MarkErrorAsync(account, message, cancellationToken);
            return BybitAccountSyncResult.Failed(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bybit sync failed for account {AccountId}", account.Id);
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

            var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, account.UserId, account.Id, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, account.UserId, account.Id, "api-secret", cancellationToken);
            if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
                return BybitAccountSyncResult.Failed(KeyVaultSecretReadResult.UnavailableMessage, 503);

            if (string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
                return BybitAccountSyncResult.Skipped("Bybit credentials are not configured for this account");

            var region = BybitEndpoints.Parse(syncStatus.Region);
            var events = new List<BackfillEvent>();
            var windowStart = start;

            while (windowStart < end)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var windowEnd = windowStart.AddDays(7) < end ? windowStart.AddDays(7) : end;
                var startTime = windowStart.ToUnixTimeMilliseconds();

                var orders = await _bybitService.GetOrderHistoryAsync(
                    apiKey.Value, apiSecret.Value, region, 50, startTime, cancellationToken, windowEnd.ToUnixTimeMilliseconds());
                foreach (var order in orders.Where(order => order.OrderStatus == "Filled")
                    .Where(order => TryGetTimestamp(order, out var timestamp) && timestamp >= windowStart && timestamp < windowEnd))
                {
                    TryGetTimestamp(order, out var timestamp);
                    events.Add(new BackfillEvent(timestamp, BackfillEventKind.Order, order));
                }

                var deposits = await _bybitService.GetDepositHistoryAsync(
                    apiKey.Value, apiSecret.Value, region, 50, startTime, cancellationToken, windowEnd.ToUnixTimeMilliseconds());
                foreach (var deposit in deposits
                    .Where(deposit => TryGetTimestamp(deposit.SuccessAt, out var timestamp) && timestamp >= windowStart && timestamp < windowEnd))
                {
                    TryGetTimestamp(deposit.SuccessAt, out var timestamp);
                    events.Add(new BackfillEvent(timestamp, BackfillEventKind.Deposit, deposit));
                }

                var withdrawals = await _bybitService.GetWithdrawalHistoryAsync(
                    apiKey.Value, apiSecret.Value, region, 50, startTime, cancellationToken, windowEnd.ToUnixTimeMilliseconds());
                foreach (var withdrawal in withdrawals
                    .Where(withdrawal => TryGetTimestamp(withdrawal, out var timestamp) && timestamp >= windowStart && timestamp < windowEnd))
                {
                    TryGetTimestamp(withdrawal, out var timestamp);
                    events.Add(new BackfillEvent(timestamp, BackfillEventKind.Withdrawal, withdrawal));
                }

                var transfers = await _bybitService.GetInternalTransferHistoryAsync(
                    apiKey.Value, apiSecret.Value, region, 50, startTime, cancellationToken, windowEnd.ToUnixTimeMilliseconds());
                foreach (var transfer in transfers
                    .Where(transfer => TryGetTimestamp(transfer.Timestamp, out var timestamp) && timestamp >= windowStart && timestamp < windowEnd))
                {
                    TryGetTimestamp(transfer.Timestamp, out var timestamp);
                    events.Add(new BackfillEvent(timestamp, BackfillEventKind.InternalTransfer, transfer));
                }

                var universalTransfers = await _bybitService.GetUniversalTransferHistoryAsync(
                    apiKey.Value, apiSecret.Value, region, 50, startTime, cancellationToken, windowEnd.ToUnixTimeMilliseconds());
                foreach (var transfer in universalTransfers
                    .Where(transfer => TryGetTimestamp(transfer.Timestamp, out var timestamp) && timestamp >= windowStart && timestamp < windowEnd))
                {
                    TryGetTimestamp(transfer.Timestamp, out var timestamp);
                    events.Add(new BackfillEvent(timestamp, BackfillEventKind.UniversalTransfer, transfer));
                }

                windowStart = windowEnd;
            }

            var processed = 0;
            string? lastOrderId = null;
            foreach (var backfillEvent in events.OrderBy(item => item.Timestamp).ThenBy(item => item.Kind))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var succeeded = backfillEvent.Kind switch
                {
                    BackfillEventKind.Order => await ProcessBackfillOrderAsync((BybitOrderData)backfillEvent.Value, account, region, apiKey.Value, apiSecret.Value, cancellationToken),
                    BackfillEventKind.Deposit => await _orderSyncService.ProcessDepositAsync((BybitDepositWithdrawalRow)backfillEvent.Value, account, account.UserId, cancellationToken, applyCashBalance: false),
                    BackfillEventKind.Withdrawal => await _orderSyncService.ProcessWithdrawalAsync((BybitDepositWithdrawalRow)backfillEvent.Value, account, account.UserId, cancellationToken, applyCashBalance: false),
                    BackfillEventKind.InternalTransfer => await _orderSyncService.ProcessInternalTransferAsync((BybitInternalTransferRow)backfillEvent.Value, account, account.UserId, cancellationToken, applyCashBalance: false),
                    BackfillEventKind.UniversalTransfer => await _orderSyncService.ProcessInternalTransferAsync((BybitInternalTransferRow)backfillEvent.Value, account, account.UserId, cancellationToken, applyCashBalance: false),
                    _ => false
                };

                if (!succeeded)
                    return BybitAccountSyncResult.Failed("One or more historical items failed to process; the import can be retried safely.");

                if (backfillEvent.Value is BybitOrderData order)
                    lastOrderId = order.OrderId;
                processed++;
            }

            await ReconcileCurrentCryptoBalancesAsync(account, apiKey.Value, apiSecret.Value, region, cancellationToken);

            await _orderSyncService.UpsertSyncStatusAsync(account.UserId, account.Id, lastOrderId, cancellationToken);
            return BybitAccountSyncResult.Succeeded($"Imported {processed} Bybit historical items without changing the cash balance.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (BybitApiException ex)
        {
            return BybitAccountSyncResult.Failed($"Bybit rejected the historical import: {ex.RetCode} - {ex.RetMsg}");
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
            return BybitAccountSyncResult.Failed($"Bybit rejected balance reconciliation: {ex.RetCode} - {ex.RetMsg}");
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
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

    private async Task<bool> ProcessBackfillOrderAsync(
        BybitOrderData order,
        Account account,
        BybitRegion region,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<BybitExecutionData> executions = [];
        try
        {
            executions = await _bybitService.GetExecutionHistoryAsync(apiKey, apiSecret, region, order.OrderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bybit historical import: execution history unavailable for order {OrderId}; using order fee details", order.OrderId);
        }

        return await _orderSyncService.ProcessOrderAsync(
            order,
            account,
            account.UserId,
            "BybitBackfill",
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

    private enum BackfillEventKind
    {
        Order,
        Deposit,
        Withdrawal,
        InternalTransfer,
        UniversalTransfer
    }

    private sealed record BackfillEvent(DateTimeOffset Timestamp, BackfillEventKind Kind, object Value);

    private async Task PopulateInitialCashBalanceAsync(
        Account account,
        string apiKey,
        string apiSecret,
        BybitRegion region,
        CancellationToken cancellationToken)
    {
        try
        {
            var balance = 0m;
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
                        balance += BybitCashBalance.FromAccountCoinBalance(coinBalance);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bybit sync: failed to fetch {AccountType} {Coin} balance for account {AccountId}",
                            accountType, coin, account.Id);
                    }
                }
            }

            await _orderSyncService.ProcessOpeningBalanceAsync(account, account.UserId, balance, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bybit sync: failed to fetch initial cash balance for account {AccountId}", account.Id);
        }
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
}
