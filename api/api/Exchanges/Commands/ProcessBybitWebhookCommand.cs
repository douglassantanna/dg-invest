using api.AzureKeyVault;
using api.AzureStorage;
using api.AzureStorage.Blob;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Models;
using api.Models.Cryptos;
using api.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Exchanges.Commands;

public record ProcessBybitWebhookCommand(
    int UserId,
    int AccountId,
    BybitWebhookPayload Payload,
    string RawBody,
    string Signature,
    string Timestamp) : IRequest<Response>;

public class ProcessBybitWebhookCommandHandler : IRequestHandler<ProcessBybitWebhookCommand, Response>
{
    private static readonly string[] KnownQuoteCurrencies = ["USDT", "USDC", "BUSD", "USD", "BTC", "ETH", "BNB"];

    private readonly IBybitService _bybitService;
    private readonly IKeyVaultService _keyVaultService;
    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly ITransactionService _transactionService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly AzureStorageSettings _storageSettings;
    private readonly DataContext _context;
    private readonly ILogger<ProcessBybitWebhookCommandHandler> _logger;

    public ProcessBybitWebhookCommandHandler(
        IBybitService bybitService,
        IKeyVaultService keyVaultService,
        ICoinMarketCapService coinMarketCapService,
        ITransactionService transactionService,
        IBlobStorageService blobStorageService,
        IOptions<AzureStorageSettings> storageSettings,
        DataContext context,
        ILogger<ProcessBybitWebhookCommandHandler> logger)
    {
        _bybitService = bybitService;
        _keyVaultService = keyVaultService;
        _coinMarketCapService = coinMarketCapService;
        _transactionService = transactionService;
        _blobStorageService = blobStorageService;
        _storageSettings = storageSettings.Value;
        _context = context;
        _logger = logger;
    }

    public async Task<Response> Handle(ProcessBybitWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate HMAC signature.
        var webhookSecret = await _keyVaultService.GetSecretAsync(
            SaveBybitCredentialsCommandHandler.BuildKey(request.UserId, request.AccountId, "webhook-secret"));

        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogWarning("ProcessBybitWebhook: webhook secret not configured for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Webhook secret not configured", false, 401);
        }

        if (!_bybitService.ValidateWebhookSignature(request.RawBody, request.Signature, request.Timestamp, webhookSecret))
        {
            _logger.LogWarning("ProcessBybitWebhook: invalid signature for user {UserId}, account {AccountId}", request.UserId, request.AccountId);
            return new Response("Invalid signature", false, 401);
        }

        // Only process order fill events.
        if (!request.Payload.Topic.Equals("order", StringComparison.OrdinalIgnoreCase))
            return new Response("ok", true);

        var account = await _context.Accounts
            .Include(a => a.CryptoAssets)
                .ThenInclude(ca => ca.Transactions)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == request.UserId, cancellationToken);

        if (account == null)
        {
            _logger.LogError("ProcessBybitWebhook: account {AccountId} not found for user {UserId}", request.AccountId, request.UserId);
            await MarkSyncStatusErrorAsync(request.UserId, request.AccountId, "Account not found", cancellationToken);
            return new Response("Account not found", false, 404);
        }

        var filledOrders = request.Payload.Data.Where(o => o.OrderStatus == "Filled").ToList();

        foreach (var order in filledOrders)
        {
            await ProcessOrderAsync(order, account, request.UserId, cancellationToken);
        }

        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);

        var lastOrderId = filledOrders.LastOrDefault()?.OrderId;
        await UpsertSyncStatusAsync(request.UserId, request.AccountId, lastOrderId, cancellationToken);

        return new Response("ok", true);
    }

    private async Task ProcessOrderAsync(BybitOrderData order, Account account, int userId, CancellationToken cancellationToken)
    {
        var baseSymbol = ExtractBaseSymbol(order.Symbol);
        var logId = Guid.NewGuid().ToString();

        // Skip duplicates — same order may arrive more than once.
        var alreadyProcessed = await _context.CryptoTransactions
            .AnyAsync(t => t.ExchangeOrderId == order.OrderId, cancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("ProcessBybitWebhook: order {OrderId} already saved, skipping", order.OrderId);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Duplicate", null, logId, cancellationToken);
            return;
        }

        if (!TryParseOrderValues(order, out var price, out var qty, out var fee))
        {
            _logger.LogError("ProcessBybitWebhook: could not parse numeric values for order {OrderId}", order.OrderId);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Failed", "Could not parse numeric values", logId, cancellationToken);
            return;
        }

        var cryptoAsset = await FindOrCreateCryptoAssetAsync(account, baseSymbol, cancellationToken);
        if (cryptoAsset == null)
        {
            _logger.LogError("ProcessBybitWebhook: could not resolve crypto asset for symbol {Symbol}", baseSymbol);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Failed", $"Could not resolve asset for symbol {baseSymbol}", logId, cancellationToken);
            return;
        }

        var transactionType = order.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase)
            ? ETransactionType.Buy
            : ETransactionType.Sell;

        var purchaseDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(order.CreatedTime));
        var cryptoTx = new CryptoTransaction(qty, price, purchaseDate, "Bybit", transactionType, fee, order.OrderId);

        var accountTransactionType = transactionType == ETransactionType.Buy
            ? EAccountTransactionType.Out
            : EAccountTransactionType.In;

        var accountTx = new AccountTransaction(
            date: purchaseDate.DateTime,
            transactionType: accountTransactionType,
            amount: qty,
            cryptoCurrentPrice: price,
            exchangeName: "Bybit",
            notes: $"Auto-synced from Bybit order {order.OrderId}",
            cryptoAssetId: cryptoAsset.Id,
            cryptoAsset: cryptoAsset,
            fee: fee);

        var result = _transactionService.ExecuteTransaction(account, accountTx);
        if (!result.IsSuccess)
        {
            _logger.LogError("ProcessBybitWebhook: transaction strategy failed for order {OrderId}: {Message}", order.OrderId, result.Message);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Failed", result.Message, logId, cancellationToken);
            return;
        }

        cryptoAsset.AddTransaction(cryptoTx);

        await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Success", null, logId, cancellationToken);
        _logger.LogInformation("ProcessBybitWebhook: saved order {OrderId} ({Side} {Qty} {Symbol} @ {Price})", order.OrderId, order.Side, qty, baseSymbol, price);
    }

    private async Task WriteSyncLogAsync(BybitOrderData order, string symbol, int userId, int accountId, string status, string? errorMessage, string logId, CancellationToken cancellationToken)
    {
        var parsed = TryParseOrderValues(order, out var price, out var qty, out _);
        var entry = new SyncLogEntry(
            Id: logId,
            UserId: userId,
            AccountId: accountId,
            ExchangeName: "Bybit",
            OrderId: order.OrderId,
            Symbol: symbol,
            Side: order.Side,
            Qty: parsed ? qty : 0,
            Price: parsed ? price : 0,
            Status: status,
            ErrorMessage: errorMessage,
            Timestamp: DateTime.UtcNow,
            ImportSource: "Webhook");

        var blobPath = $"{userId}/{accountId}/{DateTime.UtcNow:yyyy-MM-dd}.jsonl";
        await _blobStorageService.AppendLogAsync(_storageSettings.SyncLogsContainer, blobPath, entry, cancellationToken);
    }

    private async Task UpsertSyncStatusAsync(int userId, int accountId, string? lastOrderId, CancellationToken cancellationToken)
    {
        var status = await _context.SyncStatuses
            .FirstOrDefaultAsync(s => s.UserId == userId && s.AccountId == accountId && s.ExchangeName == "Bybit", cancellationToken);

        if (status == null)
        {
            status = new SyncStatus(userId, accountId, "Bybit");
            _context.SyncStatuses.Add(status);
        }

        status.MarkConnected(lastOrderId ?? string.Empty);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkSyncStatusErrorAsync(int userId, int accountId, string errorMessage, CancellationToken cancellationToken)
    {
        var status = await _context.SyncStatuses
            .FirstOrDefaultAsync(s => s.UserId == userId && s.AccountId == accountId && s.ExchangeName == "Bybit", cancellationToken);

        if (status == null)
        {
            status = new SyncStatus(userId, accountId, "Bybit");
            _context.SyncStatuses.Add(status);
        }

        status.MarkError(errorMessage);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<CryptoAsset?> FindOrCreateCryptoAssetAsync(Account account, string symbol, CancellationToken cancellationToken)
    {
        var existing = account.CryptoAssets
            .FirstOrDefault(ca => ca.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        // Auto-create from CoinMarketCap data.
        try
        {
            var quote = await _coinMarketCapService.GetQuoteBySymbol(symbol.ToUpperInvariant());
            if (quote?.Data == null || !quote.Data.Any())
            {
                _logger.LogError("ProcessBybitWebhook: CoinMarketCap returned no data for symbol {Symbol}", symbol);
                return null;
            }

            var coin = quote.Data.First().Value;
            var newAsset = new CryptoAsset(coin.Name, coin.Name, coin.Symbol, coin.Id);
            var addResult = account.AddCryptoAsset(newAsset);

            if (!addResult.IsSuccess)
            {
                _logger.LogError("ProcessBybitWebhook: could not add crypto asset {Symbol}: {Message}", symbol, addResult.Message);
                return null;
            }

            // Persist the new asset so it gets an ID before the transaction is linked.
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("ProcessBybitWebhook: auto-created crypto asset {Symbol} (CMC ID {Id})", coin.Symbol, coin.Id);
            return newAsset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessBybitWebhook: error creating crypto asset for {Symbol}", symbol);
            return null;
        }
    }

    private static string ExtractBaseSymbol(string tradingPair)
    {
        var upper = tradingPair.ToUpperInvariant();
        foreach (var quote in KnownQuoteCurrencies)
        {
            if (upper.EndsWith(quote))
                return upper[..^quote.Length];
        }
        return upper;
    }

    private static bool TryParseOrderValues(BybitOrderData order, out decimal price, out decimal qty, out decimal fee)
    {
        price = 0; qty = 0; fee = 0;
        return decimal.TryParse(order.AvgPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price)
            && decimal.TryParse(order.CumExecQty, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out qty)
            && decimal.TryParse(order.CumExecFee, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out fee);
    }
}
