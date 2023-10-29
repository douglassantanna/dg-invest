using api.Cryptos.Models;

namespace api.Models.Cryptos;
public class CryptoAsset
{
    public int Id { get; private set; }
    public string CryptoCurrency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public decimal AveragePrice { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string CurrencyName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    private readonly List<CryptoTransaction> _transactions = new();
    public IReadOnlyCollection<CryptoTransaction> Transactions => _transactions.AsReadOnly();
    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public bool Deleted { get; private set; }
    public int CoinMarketCapId { get; private set; }

    public CryptoAsset(string cryptoCurrency,
                       string currencyName,
                       string symbol,
                       int coinMarketCapId)
    {
        CryptoCurrency = cryptoCurrency;
        CurrencyName = currencyName;
        Symbol = symbol;
        CreatedAt = DateTimeOffset.UtcNow;
        Balance = 0;
        AveragePrice = 0;
        Deleted = false;
        CoinMarketCapId = coinMarketCapId;
    }
    public void Delete()
    {
        Deleted = true;
    }
    public decimal GetAveragePrice()
    {
        return _transactions.Select(t => t.Price).Average();
    }
    public void AddAddress(Address address)
    {
        _addresses.Add(address);
    }
    public void AddTransaction(CryptoTransaction transaction)
    {
        if (transaction.TransactionType == ETransactionType.Buy)
            AddBalance(transaction.Amount);
        else if (transaction.TransactionType == ETransactionType.Sell)
            SubtractBalance(transaction.Amount);
        if (transaction.Amount > 0.0m)
            _transactions.Add(transaction);
    }
    public void AddBalance(decimal amount)
    {
        if (amount > 0.0m)
            Balance += amount;
    }
    public void SubtractBalance(decimal amount)
    {
        if (amount > 0.0m)
            Balance -= amount;
    }
}
