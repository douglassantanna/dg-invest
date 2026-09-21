using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using Microsoft.EntityFrameworkCore;

namespace api.Exchanges.Services;

public sealed class BybitAccountSyncService : IBybitAccountSyncService
{
    private static readonly TimeSpan SyncOverlap = TimeSpan.FromMinutes(5);

    private readonly IBybitService _bybitService;
    private readonly IBybitOrderSyncService _orderSyncService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<BybitAccountSyncService> _logger;

    public BybitAccountSyncService(
        IBybitService bybitService,
        IBybitOrderSyncService orderSyncService,
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<BybitAccountSyncService> logger)
    {
        _bybitService = bybitService;
        _orderSyncService = orderSyncService;
        _keyVaultService = keyVaultService;
        _context = context;
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
