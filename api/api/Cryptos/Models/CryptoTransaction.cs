using System;
using api.Shared;

namespace api.Models.Cryptos;
public class CryptoTransaction : Entity
{
    public CryptoTransaction(decimal amount,
                             decimal price,
                             DateTimeOffset purchaseDate,
                             string exchangeName,
                             ETransactionType transactionType,
                             decimal fee,
                             string? exchangeOrderId = null)
    {
        Amount = amount;
        Price = price;
        PurchaseDate = purchaseDate;
        ExchangeName = StringSanitizer.Sanitize(exchangeName);
        TransactionType = transactionType;
        Enabled = true;
        Fee = fee;
        ExchangeOrderId = exchangeOrderId;
    }
    public decimal Amount { get; private set; }
    public decimal Price { get; private set; }
    public DateTimeOffset PurchaseDate { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public ETransactionType TransactionType { get; private set; }
    public bool Enabled { get; private set; }
    public decimal Fee { get; set; }
    // Null for manual entries; set for exchange-synced trades (used for deduplication).
    public string? ExchangeOrderId { get; private set; }

    internal void Disable()
    {
        Enabled = false;
    }
    public decimal GetPercentDifference(decimal currentPrice)
    {
        if (Price == 0)
        {
            return 0;
        }
        else
        {
            decimal percentDifference = ((currentPrice - Price) / Price) * 100;
            return percentDifference;
        }
    }
}