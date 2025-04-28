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
            string notes,
            int? cryptoAssetId,
            CryptoAsset? cryptoAsset,
            decimal? fee)
    {
        Date = date;
        TransactionType = transactionType;
        Amount = amount;
        ExchangeName = StringSanitizer.Sanitize(exchangeName);
        Notes = StringSanitizer.Sanitize(notes);
        CryptoCurrentPrice = cryptoCurrentPrice;
        CryptoAssetId = cryptoAssetId;
        CryptoAsset = cryptoAsset;
        Fee = fee ?? 0;
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
        Notes = StringSanitizer.Sanitize(notes) ?? string.Empty;
    }

    public DateTime Date { get; private set; }
    public EAccountTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public decimal CryptoCurrentPrice { get; private set; }
    public int? CryptoAssetId { get; private set; }
    public CryptoAsset? CryptoAsset { get; set; }
    public decimal Fee { get; private set; }
}
