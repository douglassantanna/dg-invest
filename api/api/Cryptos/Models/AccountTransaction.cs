using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.Models;
public class AccountTransaction : Entity
{

    public AccountTransaction()
    {

    }
    public AccountTransaction(
            DateTime date,
            EAccountTransactionType transactionType,
            decimal amount,
            decimal cryptoCurrentPrice,
            string exchangeName,
            string currency,
            string destination,
            string notes,
            int? cryptoAssetId,
            CryptoAsset? cryptoAsset)
    {
        Date = date;
        TransactionType = transactionType;
        Amount = amount;
        ExchangeName = exchangeName;
        Currency = currency;
        Destination = destination;
        Notes = notes;
        CryptoCurrentPrice = cryptoCurrentPrice;
        CryptoAssetId = cryptoAssetId;
        CryptoAsset = cryptoAsset;
    }
    public AccountTransaction(
            DateTime date,
            EAccountTransactionType transactionType,
            decimal amount,
            string? notes)
    {
        Date = date;
        TransactionType = transactionType;
        Amount = amount;
        Notes = notes ?? string.Empty;
    }

    public DateTime Date { get; private set; }
    public EAccountTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public decimal CryptoCurrentPrice { get; private set; }
    public int? CryptoAssetId { get; private set; }
    public CryptoAsset? CryptoAsset { get; set; }
}
