using System;

namespace api.Models.Cryptos;
public class CryptoTransaction
{
    public CryptoTransaction(decimal amount,
                             decimal price,
                             DateTimeOffset purchaseDate,
                             string exchangeName,
                             ETransactionType transactionType)
    {
        Amount = amount;
        Price = price;
        PurchaseDate = purchaseDate;
        ExchangeName = exchangeName;
        TransactionType = transactionType;
        Deleted = false;
    }

    public int Id { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Price { get; private set; }
    public DateTimeOffset PurchaseDate { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public ETransactionType TransactionType { get; private set; }
    public bool Deleted { get; private set; }

}