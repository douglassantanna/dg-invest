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
            decimal? cryptoCurrentPrice,
            string? exchangeName,
            string? currency,
            string? destination,
            string? notes)
    {
        Date = date;
        TransactionType = transactionType;
        Amount = amount;
        ExchangeName = exchangeName ?? string.Empty;
        Currency = currency ?? string.Empty;
        Destination = destination ?? string.Empty;
        Notes = notes ?? string.Empty;
        CryptoCurrentPrice = cryptoCurrentPrice ?? 0;
    }

    public DateTime Date { get; private set; }
    public EAccountTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public decimal CryptoCurrentPrice { get; private set; }
}
