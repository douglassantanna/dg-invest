using api.AzureKeyVault;
using api.Exchanges.Services;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions;

public class SyncBybitOrders
{
    private readonly IBybitService _bybitService;
    private readonly IBybitOrderSyncService _orderSyncService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly DataContext _context;
    private readonly ILogger<SyncBybitOrders> _logger;
    private readonly IConfiguration _configuration;

    public SyncBybitOrders(
        IBybitService bybitService,
        IBybitOrderSyncService orderSyncService,
        IKeyVaultService keyVaultService,
        DataContext context,
        ILogger<SyncBybitOrders> logger,
        IConfiguration configuration)
    {
        _bybitService = bybitService;
        _orderSyncService = orderSyncService;
        _keyVaultService = keyVaultService;
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [Function("SyncBybitOrders")]
    public async Task Run([TimerTrigger("*/30 * * * * *")] TimerInfo timer, FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;

        var syncEnabled = _configuration.GetValue<bool>("BybitSync:Enabled");
        if (!syncEnabled)
        {
            _logger.LogInformation("SyncBybitOrders: feature flag BybitSync:Enabled is false, skipping");
            return;
        }

        try
        {
            var accounts = await _context.Accounts
                .Include(a => a.CryptoAssets)
                    .ThenInclude(ca => ca.Transactions)
                .Where(a => a.ExternalId != null
                            && a.Enabled
                            && !a.IsDeleted
                            && a.AccountType == EAccountType.Exchange
                            && a.Exchange == "Bybit"
                            && _context.ExchangeIntegrations
                                .Where(integration => integration.UserId == a.UserId && integration.Exchange == "Bybit")
                                .All(integration => integration.Enabled))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("SyncBybitOrders: found {Count} Bybit accounts", accounts.Count);
            _logger.LogInformation("SyncBybitOrders: processing accounts {Accounts}",
                string.Join(", ", accounts.Select(a => $"user {a.UserId}/account {a.Id}/UID {a.ExternalId}")));

            if (accounts.Count == 0)
            {
                _logger.LogInformation("SyncBybitOrders: no Bybit accounts found");
                return;
            }

            foreach (var account in accounts)
            {
                await SyncAccountOrdersAsync(account, cancellationToken);
            }

            await SyncUniversalTransfersAsync(accounts, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncBybitOrders: unexpected error");
        }
    }

    private async Task SyncAccountOrdersAsync(Account account, CancellationToken cancellationToken)
    {
        try
        {
            var userId = account.UserId;
            var accountId = account.Id;

            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(s => s.UserId == userId && s.AccountId == accountId && s.ExchangeName == "Bybit", cancellationToken);
            if (syncStatus == null)
            {
                _logger.LogInformation("SyncBybitOrders: no sync status for user {UserId}, account {AccountId}, UID {ExternalId} (credentials may predate safeguard), skipping",
                    userId, accountId, account.ExternalId);
                return;
            }

            if (!syncStatus.IsEnabled)
            {
                _logger.LogInformation("SyncBybitOrders: account {AccountId}, UID {ExternalId} is disabled, skipping", accountId, account.ExternalId);
                return;
            }

            var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, accountId, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, accountId, "api-secret", cancellationToken);

            if (apiKey.IsUnavailable || apiSecret.IsUnavailable)
            {
                const string errorMessage = "Credential storage is temporarily unavailable";
                _logger.LogError("SyncBybitOrders: Key Vault unavailable for account {AccountId} (user {UserId})", accountId, userId);
                await _orderSyncService.MarkSyncStatusErrorAsync(userId, accountId, errorMessage, cancellationToken);
                return;
            }

            if (string.IsNullOrEmpty(apiKey.Value) || string.IsNullOrEmpty(apiSecret.Value))
            {
                _logger.LogInformation("SyncBybitOrders: no credentials for user {UserId}, account {AccountId}, UID {ExternalId}", userId, accountId, account.ExternalId);
                return;
            }

            var region = BybitEndpoints.Parse(syncStatus.Region);
            await PopulateInitialCashBalanceAsync(account, apiKey.Value!, apiSecret.Value!, region, cancellationToken);

            var cutoff = syncStatus.LastSyncAt ?? syncStatus.BybitCredentialsSetAt;
            var startTime = cutoff is { } dt
                ? new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeMilliseconds()
                : (long?)null;

            var orders = await _bybitService.GetOrderHistoryAsync(apiKey.Value!, apiSecret.Value!, region, limit: 50, startTime: startTime);
            var hasFailures = false;
            var filledOrders = orders.Where(o => o.OrderStatus == "Filled").ToList();
            _logger.LogInformation("SyncBybitOrders: fetched {OrderCount} orders ({FilledOrderCount} filled) for user {UserId}, account {AccountId}, UID {ExternalId}, startTime {StartTime}, statuses {Statuses}",
                orders.Count,
                filledOrders.Count,
                userId,
                accountId,
                account.ExternalId,
                startTime,
                string.Join(", ", orders.Select(order => $"{order.OrderId}:{order.OrderStatus}")));

            if (orders.Count > 0)
            {
                if (filledOrders.Count > 0)
                {
                    foreach (var order in filledOrders)
                    {
                        if (!await _orderSyncService.ProcessOrderAsync(order, account, userId, "RestPoll", cancellationToken))
                            hasFailures = true;
                    }
                    _logger.LogInformation("SyncBybitOrders: processed {Count} orders for account {AccountId}", filledOrders.Count, accountId);
                }
            }

            var deposits = await _bybitService.GetDepositHistoryAsync(apiKey.Value!, apiSecret.Value!, region, limit: 50, startTime: startTime);
            if (deposits.Count > 0)
                _logger.LogInformation("SyncBybitOrders: received {Count} deposits from Bybit for account {AccountId}: {TxIds}",
                    deposits.Count, accountId, string.Join(", ", deposits.Select(d => $"{d.TxId}({d.Status})")));

            foreach (var deposit in deposits)
            {
                if (!await _orderSyncService.ProcessDepositAsync(deposit, account, userId, cancellationToken))
                    hasFailures = true;
            }
            if (deposits.Count > 0)
                _logger.LogInformation("SyncBybitOrders: finished processing {Count} deposits for account {AccountId}", deposits.Count, accountId);

            var withdrawals = await _bybitService.GetWithdrawalHistoryAsync(apiKey.Value!, apiSecret.Value!, region, limit: 50, startTime: startTime);
            if (withdrawals.Count > 0)
                _logger.LogInformation("SyncBybitOrders: received {Count} withdrawals from Bybit for account {AccountId}: {TxIds}",
                    withdrawals.Count, accountId, string.Join(", ", withdrawals.Select(w => $"{w.TxId}({w.Status})")));

            foreach (var withdrawal in withdrawals)
            {
                if (!await _orderSyncService.ProcessWithdrawalAsync(withdrawal, account, userId, cancellationToken))
                    hasFailures = true;
            }
            if (withdrawals.Count > 0)
                _logger.LogInformation("SyncBybitOrders: finished processing {Count} withdrawals for account {AccountId}", withdrawals.Count, accountId);

            var internalTransfers = await _bybitService.GetInternalTransferHistoryAsync(apiKey.Value!, apiSecret.Value!, region, limit: 50, startTime: startTime);
            if (internalTransfers.Count > 0)
            {
                _logger.LogInformation("SyncBybitOrders: received {Count} internal transfers from Bybit for account {AccountId}: {TransferIds}",
                    internalTransfers.Count, accountId, string.Join(", ", internalTransfers.Select(t => t.TransferId)));
                foreach (var transfer in internalTransfers)
                {
                    _logger.LogInformation(
                        "SyncBybitOrders: internal transfer {TransferId}: {Amount} {Coin}, from {FromAccountType}/{FromMemberId} to {ToAccountType}/{ToMemberId}, timestamp {Timestamp}, account {AccountId}",
                        transfer.TransferId,
                        transfer.Amount,
                        transfer.Coin,
                        transfer.FromAccountType,
                        string.IsNullOrWhiteSpace(transfer.FromMemberId) ? "main" : transfer.FromMemberId,
                        transfer.ToAccountType,
                        string.IsNullOrWhiteSpace(transfer.ToMemberId) ? "main" : transfer.ToMemberId,
                        transfer.Timestamp,
                        accountId);
                }
            }

            foreach (var internalTransfer in internalTransfers)
            {
                if (!await _orderSyncService.ProcessInternalTransferAsync(internalTransfer, account, userId, cancellationToken))
                    hasFailures = true;
            }
            if (internalTransfers.Count > 0)
                _logger.LogInformation("SyncBybitOrders: finished processing {Count} internal transfers for account {AccountId}", internalTransfers.Count, accountId);

            if (hasFailures)
            {
                _logger.LogWarning("SyncBybitOrders: one or more items failed for account {AccountId}, cursor not advanced", accountId);
            }
            else
            {
                var lastOrderId = orders.Count > 0 ? orders.Last().OrderId : null;
                await _orderSyncService.UpsertSyncStatusAsync(userId, accountId, lastOrderId, cancellationToken);
            }
        }
        catch (BybitApiException ex)
        {
            var message = $"Bybit rejected sync request: {ex.RetCode} - {ex.RetMsg}";
            _logger.LogWarning(ex, "SyncBybitOrders: Bybit rejected sync request for account {AccountId}", account.Id);
            try
            {
                await _orderSyncService.MarkSyncStatusErrorAsync(account.UserId, account.Id, message, cancellationToken);
            }
            catch { }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncBybitOrders: error syncing orders for account {AccountId}", account.Id);
            try
            {
                await _orderSyncService.MarkSyncStatusErrorAsync(account.UserId, account.Id, ex.Message, cancellationToken);
            }
            catch { }
        }
    }

    private async Task PopulateInitialCashBalanceAsync(Account account, string apiKey, string apiSecret, BybitRegion region, CancellationToken cancellationToken)
    {
        if (account.Balance != 0)
            return;

        try
        {
            var balance = 0m;
            foreach (var coin in BybitCashBalance.CashCoins)
            {
                var coinBalance = await _bybitService.GetAccountCoinBalanceAsync(apiKey, apiSecret, region, "UNIFIED", coin, account.ExternalId);
                balance += BybitCashBalance.FromAccountCoinBalance(coinBalance);
            }
            await _orderSyncService.ProcessOpeningBalanceAsync(account, account.UserId, balance, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SyncBybitOrders: failed to fetch initial cash balance for account {AccountId}", account.Id);
        }
    }

    private async Task SyncUniversalTransfersAsync(IReadOnlyCollection<Account> exchangeAccounts, CancellationToken cancellationToken)
    {
        foreach (var accountsByUser in exchangeAccounts.GroupBy(account => account.UserId))
        {
            var userId = accountsByUser.Key;
            var integration = await _context.ExchangeIntegrations
                .FirstOrDefaultAsync(i => i.UserId == userId && i.Exchange == "Bybit" && i.Enabled, cancellationToken);
            if (integration is null)
                continue;

            var apiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, null, "api-key", cancellationToken);
            var apiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, null, "api-secret", cancellationToken);
            if (apiKey is null || apiSecret is null || apiKey.IsUnavailable || apiSecret.IsUnavailable
                || string.IsNullOrWhiteSpace(apiKey.Value) || string.IsNullOrWhiteSpace(apiSecret.Value))
                continue;

            var startTime = integration.LastSyncAt is { } lastSyncAt
                ? new DateTimeOffset(lastSyncAt, TimeSpan.Zero).ToUnixTimeMilliseconds()
                : (long?)null;
            var region = BybitEndpoints.Parse(integration.Region);
            List<BybitInternalTransferRow> universalTransfers;
            try
            {
                universalTransfers = await _bybitService.GetUniversalTransferHistoryAsync(
                    apiKey.Value, apiSecret.Value, region, limit: 50, startTime: startTime);
            }
            catch (BybitApiException ex)
            {
                integration.MarkError();
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogError(ex,
                    "SyncBybitOrders: universal transfer request rejected for user {UserId}; region {Region}, testnet {UseTestnet}, error {RetCode}: {RetMsg}",
                    userId,
                    region,
                    _configuration.GetValue<bool>("BybitSettings:UseTestnet"),
                    ex.RetCode,
                    ex.RetMsg);
                continue;
            }
            catch (Exception ex)
            {
                integration.MarkError();
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogError(ex,
                    "SyncBybitOrders: universal transfer request failed for user {UserId}; region {Region}, testnet {UseTestnet}",
                    userId,
                    region,
                    _configuration.GetValue<bool>("BybitSettings:UseTestnet"));
                continue;
            }
            if (universalTransfers.Count > 0)
            {
                _logger.LogInformation("SyncBybitOrders: received {Count} universal transfers from Bybit for user {UserId}: {TransferIds}",
                    universalTransfers.Count, userId, string.Join(", ", universalTransfers.Select(t => t.TransferId)));
                foreach (var transfer in universalTransfers)
                {
                    _logger.LogInformation(
                        "SyncBybitOrders: universal transfer {TransferId}: {Amount} {Coin}, from {FromAccountType}/{FromMemberId} to {ToAccountType}/{ToMemberId}, timestamp {Timestamp}, user {UserId}",
                        transfer.TransferId,
                        transfer.Amount,
                        transfer.Coin,
                        transfer.FromAccountType,
                        string.IsNullOrWhiteSpace(transfer.FromMemberId) ? "main" : transfer.FromMemberId,
                        transfer.ToAccountType,
                        string.IsNullOrWhiteSpace(transfer.ToMemberId) ? "main" : transfer.ToMemberId,
                        transfer.Timestamp,
                        userId);
                }
            }

            var candidateAccounts = accountsByUser
                .Where(account => account.Enabled && !account.IsDeleted)
                .ToList();
            var readyAccountIds = new HashSet<int>();
            foreach (var account in candidateAccounts)
            {
                var accountApiKey = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, account.Id, "api-key", cancellationToken);
                var accountApiSecret = await BybitCredentialReader.ReadAsync(_keyVaultService, userId, account.Id, "api-secret", cancellationToken);
                if (accountApiKey.IsFound && !string.IsNullOrWhiteSpace(accountApiKey.Value)
                    && accountApiSecret.IsFound && !string.IsNullOrWhiteSpace(accountApiSecret.Value))
                    readyAccountIds.Add(account.Id);
            }
            var hasFailures = false;

            foreach (var transfer in universalTransfers)
            {
                var sourceAccount = FindTransferAccount(candidateAccounts, transfer.FromAccountType, transfer.FromMemberId);
                var destinationAccount = FindTransferAccount(candidateAccounts, transfer.ToAccountType, transfer.ToMemberId);
                if (sourceAccount is null || destinationAccount is null)
                {
                    hasFailures = true;
                    _logger.LogWarning(
                        "SyncBybitOrders: skipped universal transfer {TransferId} for user {UserId}; source UID {SourceMemberId} linked: {SourceLinked} (account {SourceAccountId}), destination UID {DestinationMemberId} linked: {DestinationLinked} (account {DestinationAccountId})",
                        transfer.TransferId,
                        userId,
                        transfer.FromMemberId,
                        sourceAccount is not null,
                        sourceAccount?.Id,
                        transfer.ToMemberId,
                        destinationAccount is not null,
                        destinationAccount?.Id);
                    continue;
                }

                if (!readyAccountIds.Contains(sourceAccount.Id) || !readyAccountIds.Contains(destinationAccount.Id))
                {
                    hasFailures = true;
                    _logger.LogWarning("SyncBybitOrders: skipped universal transfer {TransferId}; source account {SourceAccountId} or destination account {DestinationAccountId} is not credential-ready",
                        transfer.TransferId, sourceAccount.Id, destinationAccount.Id);
                    continue;
                }

                if (!await _orderSyncService.ProcessInternalTransferAsync(transfer, sourceAccount, userId, cancellationToken)
                    || !await _orderSyncService.ProcessInternalTransferAsync(transfer, destinationAccount, userId, cancellationToken))
                {
                    hasFailures = true;
                }
            }

            if (universalTransfers.Count > 0)
                _logger.LogInformation("SyncBybitOrders: finished processing {Count} universal transfers for user {UserId}", universalTransfers.Count, userId);
            if (!hasFailures)
            {
                integration.MarkSynced(DateTime.UtcNow);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static Account? FindTransferAccount(IEnumerable<Account> accounts, string accountType, string memberId) =>
        accounts.FirstOrDefault(account => account.AccountType == EAccountType.Exchange
            && (accountType.Equals("FUND", StringComparison.OrdinalIgnoreCase)
                || accountType.Equals("UNIFIED", StringComparison.OrdinalIgnoreCase))
            && string.Equals(account.ExternalId, memberId, StringComparison.Ordinal));
}
