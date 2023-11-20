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
            return GetAveragePrice();
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
        try
        {
            if (transaction.TransactionType == ETransactionType.Buy)
            {
                AddBalance(transaction.Amount);
                TotalInvested += transaction.Price * transaction.Amount;
            }
            else if (transaction.TransactionType == ETransactionType.Sell)
            {
                SubtractBalance(transaction.Amount);
                TotalInvested -= transaction.Price;

                if (Balance == 0)
                {
                    DisableActiveBuyTransactions();
                }
            }
            _transactions.Add(transaction);
        }
        catch (CryptoAssetException ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
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
        if (Balance == 0)
        {
            return 0;
        }
        else
        {
            decimal averagePrice = GetAveragePrice();
            decimal difference = currentPrice - averagePrice;
            decimal percentDifference = (difference / averagePrice) * 100;
            return percentDifference;
        }
    }
    internal decimal CurrentWorth(decimal currentPrice)
    {
        return Balance * currentPrice;
    }
    internal decimal GetInvestmentGainLoss(decimal currentPrice)
    {
        if (Balance == 0)
        {
            return 0;
        }
        var total = CurrentWorth(currentPrice) - TotalInvested;
        return total;
    }
    private decimal GetAveragePrice()
    {
        var enableTransactions = _transactions
                                .Where(t => t.TransactionType == ETransactionType.Buy)
                                .Where(t => t.Enabled == true)
                                .Select(t => t.Price)
                                .ToList();

        if (enableTransactions.Any())
        {
            decimal averagePrice = enableTransactions.Average();
            if (Balance == 0)
            {
                return 0;
            }
            return averagePrice;
        }
        return 0;
    }
    private void DisableActiveBuyTransactions()
    {
        var ativeBuyTransactions = _transactions.Where(x => x.Enabled).ToList();
        if (ativeBuyTransactions.Any())
        {
            foreach (var item in ativeBuyTransactions)
            {
                item.Disable();
            }
        }
    }
}
