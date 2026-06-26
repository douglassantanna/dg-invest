using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Services;
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
                .Where(a => a.BybitUid != null)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("SyncBybitOrders: found {Count} Bybit accounts", accounts.Count);
            _logger.LogInformation("SyncBybitOrders: processing accounts {AccountIds}", string.Join(", ", accounts.Select(a => a.Id)));

            if (accounts.Count == 0)
            {
                _logger.LogInformation("SyncBybitOrders: no Bybit accounts found");
                return;
            }

            foreach (var account in accounts)
            {
                await SyncAccountOrdersAsync(account, cancellationToken);
            }
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

            var apiKey = await _keyVaultService.GetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, accountId, "api-key"));
            var apiSecret = await _keyVaultService.GetSecretAsync(SaveBybitCredentialsCommandHandler.BuildKey(userId, accountId, "api-secret"));

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                _logger.LogInformation("SyncBybitOrders: no credentials for account {AccountId} (user {UserId})", accountId, userId);
                return;
            }

            var syncStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(s => s.UserId == userId && s.AccountId == accountId && s.ExchangeName == "Bybit", cancellationToken);
            var cutoff = syncStatus?.LastSyncAt ?? syncStatus?.BybitCredentialsSetAt;
            var startTime = cutoff is { } dt
                ? new DateTimeOffset(dt, TimeSpan.Zero).ToUnixTimeMilliseconds()
                : (long?)null;

            var orders = await _bybitService.GetOrderHistoryAsync(apiKey, apiSecret, limit: 50, startTime: startTime);
            if (orders.Count > 0)
            {
                var filledOrders = orders.Where(o => o.OrderStatus == "Filled").ToList();
                if (filledOrders.Count > 0)
                {
                    foreach (var order in filledOrders)
                    {
                        await _orderSyncService.ProcessOrderAsync(order, account, userId, "RestPoll", cancellationToken);
                    }
                    _logger.LogInformation("SyncBybitOrders: processed {Count} orders for account {AccountId}", filledOrders.Count, accountId);
                }
            }
            else
            {
                _logger.LogInformation("SyncBybitOrders: no orders for account {AccountId}", accountId);
            }

            var deposits = await _bybitService.GetDepositHistoryAsync(apiKey, apiSecret, limit: 50, startTime: startTime);
            _logger.LogInformation("SyncBybitOrders: received {Count} deposits from Bybit for account {AccountId}: {TxIds}",
                deposits.Count, accountId, string.Join(", ", deposits.Select(d => $"{d.TxId}({d.Status})")));

            foreach (var deposit in deposits)
            {
                await _orderSyncService.ProcessDepositAsync(deposit, account, userId, cancellationToken);
            }
            _logger.LogInformation("SyncBybitOrders: finished processing {Count} deposits for account {AccountId}", deposits.Count, accountId);

            var withdrawals = await _bybitService.GetWithdrawalHistoryAsync(apiKey, apiSecret, limit: 50, startTime: startTime);
            _logger.LogInformation("SyncBybitOrders: received {Count} withdrawals from Bybit for account {AccountId}: {TxIds}",
                withdrawals.Count, accountId, string.Join(", ", withdrawals.Select(w => $"{w.TxId}({w.Status})")));

            foreach (var withdrawal in withdrawals)
            {
                await _orderSyncService.ProcessWithdrawalAsync(withdrawal, account, userId, cancellationToken);
            }
            _logger.LogInformation("SyncBybitOrders: finished processing {Count} withdrawals for account {AccountId}", withdrawals.Count, accountId);

            var lastOrderId = orders.Count > 0 ? orders.Last().OrderId : null;
            await _orderSyncService.UpsertSyncStatusAsync(userId, accountId, lastOrderId, cancellationToken);
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
}
