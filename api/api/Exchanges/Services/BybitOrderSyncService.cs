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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Exchanges.Services;

public class BybitOrderSyncService : IBybitOrderSyncService
{
    private static readonly string[] KnownQuoteCurrencies = ["USDT", "USDC", "BUSD", "USD", "BTC", "ETH", "BNB"];

    private readonly ICoinMarketCapService _coinMarketCapService;
    private readonly ITransactionService _transactionService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly AzureStorageSettings _storageSettings;
    private readonly DataContext _context;
    private readonly ILogger<BybitOrderSyncService> _logger;

    public BybitOrderSyncService(
        ICoinMarketCapService coinMarketCapService,
        ITransactionService transactionService,
        IBlobStorageService blobStorageService,
        IOptions<AzureStorageSettings> storageSettings,
        DataContext context,
        ILogger<BybitOrderSyncService> logger)
    {
        _coinMarketCapService = coinMarketCapService;
        _transactionService = transactionService;
        _blobStorageService = blobStorageService;
        _storageSettings = storageSettings.Value;
        _context = context;
        _logger = logger;
    }

    public async Task ProcessOrderAsync(BybitOrderData order, Account account, int userId, string importSource, CancellationToken cancellationToken)
    {
        var baseSymbol = ExtractBaseSymbol(order.Symbol);
        var logId = Guid.NewGuid().ToString();

        var alreadyProcessed = await _context.CryptoTransactions
            .AnyAsync(t => t.ExchangeOrderId == order.OrderId, cancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("Bybit sync: order {OrderId} already saved, skipping", order.OrderId);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Duplicate", null, importSource, logId, cancellationToken);
            return;
        }

        if (!TryParseOrderValues(order, out var price, out var qty, out var fee))
        {
            _logger.LogError("Bybit sync: could not parse numeric values for order {OrderId}", order.OrderId);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Failed", "Could not parse numeric values", importSource, logId, cancellationToken);
            return;
        }

        var cryptoAsset = await FindOrCreateCryptoAssetAsync(account, baseSymbol, cancellationToken);
        if (cryptoAsset == null)
        {
            _logger.LogError("Bybit sync: could not resolve crypto asset for symbol {Symbol}", baseSymbol);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Failed", $"Could not resolve asset for symbol {baseSymbol}", importSource, logId, cancellationToken);
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
            _logger.LogError("Bybit sync: transaction strategy failed for order {OrderId}: {Message}", order.OrderId, result.Message);
            await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Failed", result.Message, importSource, logId, cancellationToken);
            return;
        }

        cryptoAsset.AddTransaction(cryptoTx);

        await WriteSyncLogAsync(order, baseSymbol, userId, account.Id, "Success", null, importSource, logId, cancellationToken);
        _logger.LogInformation("Bybit sync: saved order {OrderId} ({Side} {Qty} {Symbol} @ {Price})", order.OrderId, order.Side, qty, baseSymbol, price);
    }

    public async Task ProcessDepositAsync(BybitDepositWithdrawalRow deposit, Account account, int userId, CancellationToken cancellationToken)
    {
        var logId = Guid.NewGuid().ToString();
        var symbol = deposit.Coin;

        if (!TryParseDepositWithdrawalAmount(deposit, out var amount))
        {
            _logger.LogError("Bybit sync: could not parse amount for deposit {TxId}", deposit.TxId);
            await WriteDepositWithdrawalSyncLogAsync(deposit, symbol, userId, account.Id, "Failed", "Could not parse amount", "BybitDeposit", logId, cancellationToken);
            return;
        }

        var alreadyProcessed = await _context.AccountTransactions
            .AnyAsync(t => t.ExchangeTransactionId == deposit.TxId, cancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("Bybit sync: deposit {TxId} already saved, skipping", deposit.TxId);
            await WriteDepositWithdrawalSyncLogAsync(deposit, symbol, userId, account.Id, "Duplicate", null, "BybitDeposit", logId, cancellationToken);
            return;
        }

        var cryptoAsset = await FindOrCreateCryptoAssetAsync(account, symbol, cancellationToken);
        if (cryptoAsset == null)
        {
            _logger.LogError("Bybit sync: could not resolve crypto asset for deposit symbol {Symbol}", symbol);
            await WriteDepositWithdrawalSyncLogAsync(deposit, symbol, userId, account.Id, "Failed", $"Could not resolve asset for symbol {symbol}", "BybitDeposit", logId, cancellationToken);
            return;
        }

        var successAt = DateTimeOffset.TryParse(deposit.SuccessAt, out var parsed)
            ? parsed.DateTime
            : DateTime.UtcNow;

        var depositPrice = await GetMarketPriceAsync(symbol, cancellationToken);

        var accountTx = new AccountTransaction(
            date: successAt,
            transactionType: EAccountTransactionType.DepositCrypto,
            amount: amount,
            cryptoCurrentPrice: depositPrice,
            exchangeName: "Bybit",
            notes: $"Auto-synced from Bybit deposit {deposit.TxId}",
            cryptoAssetId: cryptoAsset.Id,
            cryptoAsset: cryptoAsset,
            fee: 0,
            exchangeTransactionId: deposit.TxId);

        var result = _transactionService.ExecuteTransaction(account, accountTx);
        if (!result.IsSuccess)
        {
            _logger.LogError("Bybit sync: transaction strategy failed for deposit {TxId}: {Message}", deposit.TxId, result.Message);
            await WriteDepositWithdrawalSyncLogAsync(deposit, symbol, userId, account.Id, "Failed", result.Message, "BybitDeposit", logId, cancellationToken);
            return;
        }

        await WriteDepositWithdrawalSyncLogAsync(deposit, symbol, userId, account.Id, "Success", null, "BybitDeposit", logId, cancellationToken);
        _logger.LogInformation("Bybit sync: saved deposit {TxId} ({Amount} {Symbol})", deposit.TxId, amount, symbol);
    }

    public async Task ProcessWithdrawalAsync(BybitDepositWithdrawalRow withdrawal, Account account, int userId, CancellationToken cancellationToken)
    {
        var logId = Guid.NewGuid().ToString();
        var symbol = withdrawal.Coin;

        if (!TryParseDepositWithdrawalAmount(withdrawal, out var amount))
        {
            _logger.LogError("Bybit sync: could not parse amount for withdrawal {TxId}", withdrawal.TxId);
            await WriteDepositWithdrawalSyncLogAsync(withdrawal, symbol, userId, account.Id, "Failed", "Could not parse amount", "BybitWithdrawal", logId, cancellationToken);
            return;
        }

        if (!decimal.TryParse(withdrawal.WithdrawFee, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fee))
        {
            fee = 0;
        }

        var alreadyProcessed = await _context.AccountTransactions
            .AnyAsync(t => t.ExchangeTransactionId == withdrawal.TxId, cancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("Bybit sync: withdrawal {TxId} already saved, skipping", withdrawal.TxId);
            await WriteDepositWithdrawalSyncLogAsync(withdrawal, symbol, userId, account.Id, "Duplicate", null, "BybitWithdrawal", logId, cancellationToken);
            return;
        }

        var cryptoAsset = await FindOrCreateCryptoAssetAsync(account, symbol, cancellationToken);
        if (cryptoAsset == null)
        {
            _logger.LogError("Bybit sync: could not resolve crypto asset for withdrawal symbol {Symbol}", symbol);
            await WriteDepositWithdrawalSyncLogAsync(withdrawal, symbol, userId, account.Id, "Failed", $"Could not resolve asset for symbol {symbol}", "BybitWithdrawal", logId, cancellationToken);
            return;
        }

        var successAt = DateTimeOffset.TryParse(withdrawal.SuccessAt, out var parsed)
            ? parsed.DateTime
            : DateTime.UtcNow;

        var withdrawalPrice = await GetMarketPriceAsync(symbol, cancellationToken);

        var accountTx = new AccountTransaction(
            date: successAt,
            transactionType: EAccountTransactionType.WithdrawCrypto,
            amount: amount,
            cryptoCurrentPrice: withdrawalPrice,
            exchangeName: "Bybit",
            notes: $"Auto-synced from Bybit withdrawal {withdrawal.TxId}",
            cryptoAssetId: cryptoAsset.Id,
            cryptoAsset: cryptoAsset,
            fee: fee,
            exchangeTransactionId: withdrawal.TxId);

        var result = _transactionService.ExecuteTransaction(account, accountTx);
        if (!result.IsSuccess)
        {
            _logger.LogError("Bybit sync: transaction strategy failed for withdrawal {TxId}: {Message}", withdrawal.TxId, result.Message);
            await WriteDepositWithdrawalSyncLogAsync(withdrawal, symbol, userId, account.Id, "Failed", result.Message, "BybitWithdrawal", logId, cancellationToken);
            return;
        }

        await WriteDepositWithdrawalSyncLogAsync(withdrawal, symbol, userId, account.Id, "Success", null, "BybitWithdrawal", logId, cancellationToken);
        _logger.LogInformation("Bybit sync: saved withdrawal {TxId} ({Amount} {Symbol})", withdrawal.TxId, amount, symbol);
    }

    public async Task UpsertSyncStatusAsync(int userId, int accountId, string? lastOrderId, CancellationToken cancellationToken)
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

    public async Task MarkSyncStatusErrorAsync(int userId, int accountId, string errorMessage, CancellationToken cancellationToken)
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

    private async Task WriteSyncLogAsync(BybitOrderData order, string symbol, int userId, int accountId, string status, string? errorMessage, string importSource, string logId, CancellationToken cancellationToken)
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
            ImportSource: importSource);

        var blobPath = $"{userId}/{accountId}/{DateTime.UtcNow:yyyy-MM-dd}.jsonl";
        await _blobStorageService.AppendLogAsync(_storageSettings.SyncLogsContainer, blobPath, entry, cancellationToken);
    }

    private async Task<CryptoAsset?> FindOrCreateCryptoAssetAsync(Account account, string symbol, CancellationToken cancellationToken)
    {
        var existing = account.CryptoAssets
            .FirstOrDefault(ca => ca.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        try
        {
            var quote = await _coinMarketCapService.GetQuoteBySymbol(symbol.ToUpperInvariant());
            if (quote?.Data == null || !quote.Data.Any())
            {
                _logger.LogError("Bybit sync: CoinMarketCap returned no data for symbol {Symbol}", symbol);
                return null;
            }

            var coin = quote.Data.First().Value;
            var newAsset = new CryptoAsset(coin.Name, coin.Name, coin.Symbol, coin.Id);
            var addResult = account.AddCryptoAsset(newAsset);

            if (!addResult.IsSuccess)
            {
                _logger.LogError("Bybit sync: could not add crypto asset {Symbol}: {Message}", symbol, addResult.Message);
                return null;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bybit sync: auto-created crypto asset {Symbol} (CMC ID {Id})", coin.Symbol, coin.Id);
            return newAsset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bybit sync: error creating crypto asset for {Symbol}", symbol);
            return null;
        }
    }

    internal static string ExtractBaseSymbol(string tradingPair)
    {
        var upper = tradingPair.ToUpperInvariant();
        foreach (var quote in KnownQuoteCurrencies)
        {
            if (upper.EndsWith(quote))
                return upper[..^quote.Length];
        }
        return upper;
    }

    internal static bool TryParseOrderValues(BybitOrderData order, out decimal price, out decimal qty, out decimal fee)
    {
        price = 0; qty = 0; fee = 0;
        return decimal.TryParse(order.AvgPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out price)
            && decimal.TryParse(order.CumExecQty, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out qty)
            && decimal.TryParse(order.CumExecFee, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out fee);
    }

    private async Task WriteDepositWithdrawalSyncLogAsync(BybitDepositWithdrawalRow row, string symbol, int userId, int accountId, string status, string? errorMessage, string importSource, string logId, CancellationToken cancellationToken)
    {
        _ = decimal.TryParse(row.Amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var qty);
        var entry = new SyncLogEntry(
            Id: logId,
            UserId: userId,
            AccountId: accountId,
            ExchangeName: "Bybit",
            OrderId: row.TxId,
            Symbol: symbol,
            Side: string.Empty,
            Qty: qty,
            Price: 0,
            Status: status,
            ErrorMessage: errorMessage,
            Timestamp: DateTime.UtcNow,
            ImportSource: importSource);

        var blobPath = $"{userId}/{accountId}/{DateTime.UtcNow:yyyy-MM-dd}.jsonl";
        await _blobStorageService.AppendLogAsync(_storageSettings.SyncLogsContainer, blobPath, entry, cancellationToken);
    }

    private static bool TryParseDepositWithdrawalAmount(BybitDepositWithdrawalRow row, out decimal amount)
    {
        return decimal.TryParse(row.Amount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount);
    }

    private async Task<decimal> GetMarketPriceAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var quote = await _coinMarketCapService.GetQuoteBySymbol(symbol.ToUpperInvariant());
            if (quote?.Data != null && quote.Data.Any())
            {
                var coin = quote.Data.First().Value;
                return coin.Quote?.USD?.Price ?? 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch market price for {Symbol}, using 0", symbol);
        }
        return 0;
    }
}
