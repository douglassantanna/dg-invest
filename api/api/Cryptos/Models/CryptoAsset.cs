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

    public void AddTransaction(CryptoTransaction transaction, TransferCostPolicy policy = TransferCostPolicy.Legacy)
    {
        switch (transaction.TransactionType)
        {
            case ETransactionType.Buy:
                HandleBuyTransaction(transaction);
                break;
            case ETransactionType.Sell:
                HandleSellTransaction(transaction);
                break;
            case ETransactionType.TransferIn:
                HandleTransferIn(transaction, policy);
                break;
            case ETransactionType.TransferOut:
                HandleTransferOut(transaction, policy);
                break;
            default:
                throw new CryptoAssetException("Invalid transaction type");
        }

        _transactions.Add(transaction);
    }

    public void AddHistoricalTransaction(CryptoTransaction transaction, TransferCostPolicy policy = TransferCostPolicy.Legacy)
    {
        _transactions.Add(transaction);
        RecalculateFromTransactions(policy);
    }

    public void AddBalance(decimal amount)
    {
        if (amount > 0.0m)
            Balance += amount;
    }

    public decimal GetPercentDifference(decimal currentPrice)
    {
        if (AveragePrice == 0 || Balance == 0)
            return 0;

        return ((currentPrice - AveragePrice) / AveragePrice) * 100;
    }

    public bool HasMeaningfulInvestment(decimal minimumInvestedAmount)
        => Balance > 0 && (TotalInvested == 0 || TotalInvested >= minimumInvestedAmount);

    public void ReconcileBalance(decimal externalBalance)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(externalBalance);

        if (externalBalance == Balance)
            return;

        if (externalBalance == 0)
        {
            Balance = 0;
            TotalInvested = 0;
            return;
        }

        if (Balance > 0 && externalBalance < Balance)
            TotalInvested *= externalBalance / Balance;

        Balance = externalBalance;
    }

    public void RecalculateFromTransactions(TransferCostPolicy policy = TransferCostPolicy.Legacy)
    {
        (Balance, TotalInvested) = Replay(_transactions, policy);
    }

    // Quantity explained by the transaction history. It does not depend on the cost policy.
    public decimal HistoryBalance() => Replay(_transactions, TransferCostPolicy.Legacy).Balance;

    // Cost figures for display. Under Proportional, coins Bybit holds beyond what history explains (Balance was
    // reconciled to the wallet) are valued at the current price instead of at zero cost, so they add no gain.
    public CostBasisView GetCostBasis(decimal currentPrice, TransferCostPolicy policy)
    {
        if (policy == TransferCostPolicy.Legacy)
            return new CostBasisView(
                TotalInvested,
                AveragePrice,
                GetInvestmentGainLossValue(currentPrice),
                GetInvestmentGainLossPercentage(currentPrice),
                0m);

        return ToCostBasisView(TotalInvested, HistoryBalance(), currentPrice);
    }

    // What GetCostBasis would show after recalculating from history under the policy and reconciling back to the
    // current balance, which is what the next Bybit sync or a recalculation does. Does not change the asset.
    public CostBasisView PreviewCostBasis(decimal currentPrice, TransferCostPolicy policy)
    {
        var (historyBalance, historyInvested) = Replay(_transactions, policy);
        var invested = Balance == 0 ? 0m
            : historyBalance > 0 && Balance < historyBalance ? historyInvested * (Balance / historyBalance)
            : historyInvested;

        if (policy == TransferCostPolicy.Legacy)
        {
            var legacyGain = invested == 0 ? 0 : CurrentWorth(currentPrice) - invested;
            return new CostBasisView(invested, Balance == 0 ? 0 : invested / Balance, legacyGain,
                invested == 0 ? 0 : legacyGain / invested * 100, 0m);
        }

        return ToCostBasisView(invested, historyBalance, currentPrice);
    }

    private CostBasisView ToCostBasisView(decimal recordedInvested, decimal historyBalance, decimal currentPrice)
    {
        var unexplained = Math.Max(0m, Balance - historyBalance);
        var invested = recordedInvested + unexplained * currentPrice;
        var averagePrice = Balance == 0 ? 0 : invested / Balance;
        var gainLoss = invested == 0 ? 0 : CurrentWorth(currentPrice) - invested;
        var gainLossPercentage = invested == 0 ? 0 : gainLoss / invested * 100;
        return new CostBasisView(invested, averagePrice, gainLoss, gainLossPercentage, unexplained);
    }

    private static (decimal Balance, decimal Invested) Replay(IEnumerable<CryptoTransaction> transactions, TransferCostPolicy policy)
    {
        var balance = 0m;
        var invested = 0m;

        foreach (var t in transactions.OrderBy(t => t.PurchaseDate))
        {
            switch (t.TransactionType)
            {
                case ETransactionType.Buy:
                    balance += t.Amount;
                    invested += (t.Amount * t.Price) + t.FeeInQuoteValue;
                    break;

                case ETransactionType.Sell:
                    if (t.Amount > balance) break; // skip corrupted sells
                    invested -= t.Amount * (balance == 0 ? 0 : invested / balance);
                    balance -= t.Amount;
                    if (balance == 0) invested = 0;
                    break;

                case ETransactionType.TransferIn:
                    balance += t.Amount;
                    if (policy == TransferCostPolicy.Proportional)
                        invested += t.Amount * t.Price;
                    break;

                case ETransactionType.TransferOut:
                    if (t.Amount > balance) break; // skip transfers without a known opening balance
                    if (policy == TransferCostPolicy.Proportional)
                        invested -= t.Amount * (balance == 0 ? 0 : invested / balance);
                    balance -= t.Amount;
                    if (balance == 0) invested = 0;
                    break;
            }
        }

        return (balance, invested);
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

    private void HandleTransferIn(CryptoTransaction transaction, TransferCostPolicy policy)
    {
        if (transaction.Amount <= 0)
            throw new CryptoAssetException("Transfer amount must be greater than zero");

        Balance += transaction.Amount;
        if (policy == TransferCostPolicy.Proportional)
            TotalInvested += transaction.Amount * transaction.Price;
    }

    private void HandleTransferOut(CryptoTransaction transaction, TransferCostPolicy policy)
    {
        if (transaction.Amount <= 0)
            throw new CryptoAssetException("Transfer amount must be greater than zero");

        if (transaction.Amount > Balance)
            throw new CryptoAssetException("Insufficient balance");

        if (policy == TransferCostPolicy.Proportional)
            TotalInvested -= transaction.Amount * AveragePrice;
        Balance -= transaction.Amount;

        if (Balance == 0)
            TotalInvested = 0;
    }

    private void HandleBuyTransaction(CryptoTransaction transaction)
    {
        if (transaction.Amount <= 0)
            throw new CryptoAssetException("Buy amount must be greater than zero");

        decimal totalCost = (transaction.Amount * transaction.Price) + transaction.FeeInQuoteValue;
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
