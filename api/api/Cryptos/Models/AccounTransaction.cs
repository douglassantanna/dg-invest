using api.Shared;

namespace api.Cryptos.Models;
public class AccountTransaction : Entity
{
    public DateTime Date { get; private set; }
    public EAccountTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public string ExchangeName { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public decimal CryptoCurrentPrice { get; private set; }
}
