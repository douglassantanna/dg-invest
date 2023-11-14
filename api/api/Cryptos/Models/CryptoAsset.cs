using api.Cryptos.Exceptions;
using api.Cryptos.Models;

namespace api.Models.Cryptos;
public class CryptoAsset
{
    public int Id { get; private set; }
    public string CryptoCurrency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    private decimal _averagePrice;
    public decimal AveragePrice
    {
        get
        {
            return _transactions.Select(t => t.Price).Average();
        }
        set
        {
            _averagePrice = value;
        }
    }
    public string Symbol { get; private set; } = string.Empty;
    public string CurrencyName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    private readonly List<CryptoTransaction> _transactions = new();
    public IReadOnlyCollection<CryptoTransaction> Transactions => _transactions.AsReadOnly();
    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public bool Deleted { get; private set; }
    public int CoinMarketCapId { get; private set; }
    public decimal TotalInvested { get; private set; }

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
        Deleted = false;
        CoinMarketCapId = coinMarketCapId;
    }
    public void Delete()
    {
        Deleted = true;
    }
    public void AddAddress(Address address)
    {
        _addresses.Add(address);
    }
    public void AddTransaction(CryptoTransaction transaction)
    {
        if (transaction.TransactionType == ETransactionType.Buy)
        {
            AddBalance(transaction.Amount);
            TotalInvested += transaction.Price;
        }
        else if (transaction.TransactionType == ETransactionType.Sell)
        {
            SubtractBalance(transaction.Amount);
            TotalInvested -= transaction.Price;
        }

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
        if (amount <= 0.0m)
        {
            throw new CryptoAssetException("Amount must be greater than 0");
        }

        if (Balance < amount)
        {
            throw new CryptoAssetException("Insufficient funds");
        }

        Balance -= amount;
    }


    public decimal GetPercentDifference(decimal currentPrice)
    {
        decimal averagePrice = AveragePrice;
        if (averagePrice == 0)
        {
            if (currentPrice > 0)
            {
                return decimal.MaxValue;
            }
            else if (currentPrice < 0)
            {
                return decimal.MinValue;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            decimal difference = currentPrice - averagePrice;
            decimal percentDifference = (difference / averagePrice) * 100;
            return percentDifference;
        }
    }
    internal decimal CurrentWorth(decimal currentPrice)
    {
        return Balance * currentPrice;
    }

    internal decimal GetInvestmentGainLoss()
    {
        var total = Balance * AveragePrice;
        return total;
    }
}
