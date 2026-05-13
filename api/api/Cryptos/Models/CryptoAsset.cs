using api.Cryptos.Exceptions;
using api.Cryptos.Models;
using api.Shared;

namespace api.Models.Cryptos;

public class CryptoAsset : Entity
{
    public string CryptoCurrency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public decimal TotalInvested { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string CurrencyName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    private readonly List<CryptoTransaction> _transactions = new();
    public IReadOnlyCollection<CryptoTransaction> Transactions => _transactions.AsReadOnly();
    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public bool Deleted { get; private set; }
    public int CoinMarketCapId { get; private set; }

    public decimal AveragePrice => Balance == 0 ? 0 : TotalInvested / Balance;

    public CryptoAsset(string cryptoCurrency,
                       string currencyName,
                       string symbol,
                       int coinMarketCapId)
    {
        CryptoCurrency = cryptoCurrency;
        CurrencyName = StringSanitizer.Sanitize(currencyName);
        Symbol = symbol;
        CreatedAt = DateTimeOffset.UtcNow;
        Balance = 0;
        TotalInvested = 0;
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
        switch (transaction.TransactionType)
        {
            case ETransactionType.Buy:
                HandleBuyTransaction(transaction);
                break;
            case ETransactionType.Sell:
                HandleSellTransaction(transaction);
                break;
            default:
                throw new CryptoAssetException("Invalid transaction type");
        }

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
            throw new CryptoAssetException("Amount must be greater than 0");

        if (Balance < amount)
            throw new CryptoAssetException("Insufficient funds");

        Balance -= amount;
    }

    public decimal GetPercentDifference(decimal currentPrice)
    {
        if (AveragePrice == 0 || Balance == 0)
            return 0;

        return ((currentPrice - AveragePrice) / AveragePrice) * 100;
    }

    internal decimal CurrentWorth(decimal currentPrice)
    {
        return Balance * currentPrice;
    }

    internal decimal GetInvestmentGainLossValue(decimal currentPrice)
    {
        if (TotalInvested == 0)
            return 0;

        return CurrentWorth(currentPrice) - TotalInvested;
    }

    internal decimal GetInvestmentGainLossPercentage(decimal currentPrice)
    {
        if (TotalInvested == 0)
            return 0;

        var gainOrLoss = CurrentWorth(currentPrice) - TotalInvested;
        return (gainOrLoss / TotalInvested) * 100;
    }

    private void HandleBuyTransaction(CryptoTransaction transaction)
    {
        if (transaction.Amount <= 0)
            throw new CryptoAssetException("Buy amount must be greater than zero");

        decimal totalCost = (transaction.Amount * transaction.Price) + transaction.Fee;
        Balance += transaction.Amount;
        TotalInvested += totalCost;
    }

    private void HandleSellTransaction(CryptoTransaction transaction)
    {
        if (transaction.Amount <= 0)
            throw new CryptoAssetException("Sell amount must be greater than zero");

        if (transaction.Amount > Balance)
            throw new CryptoAssetException("Insufficient balance");

        decimal costBasisRemoved = transaction.Amount * AveragePrice;
        Balance -= transaction.Amount;
        TotalInvested -= costBasisRemoved;

        if (Balance == 0)
            TotalInvested = 0;
    }
}
