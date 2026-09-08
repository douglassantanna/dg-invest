using api.Models.Cryptos;
using api.Shared;
using api.Users.Models;

namespace api.Cryptos.Models;
public class Account : Entity
{
    public bool IsSelected { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public decimal Balance { get; private set; }
    private readonly List<AccountTransaction> _accountTransactions = new();
    private readonly List<CryptoAsset> _cryptoAssets = new();
    public IReadOnlyCollection<CryptoAsset> CryptoAssets => _cryptoAssets.AsReadOnly();
    public string Name { get; private set; } = string.Empty;
    public EAccountType AccountType { get; private set; } = EAccountType.Manual;
    public string? Exchange { get; private set; }
    public string? ExternalId { get; private set; }
    public bool Enabled { get; private set; } = true;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Account(
        string name,
        int userId,
        EAccountType accountType = EAccountType.Manual,
        string? exchange = null,
        string? externalId = null)
    {
        Name = name;
        UserId = userId;
        AccountType = accountType;
        Exchange = exchange;
        ExternalId = NormalizeExternalId(externalId);
        IsSelected = name == "main" ? true : false;
        CreatedAt = DateTime.Now;
    }

    public void SetExternalId(string externalId) => ExternalId = NormalizeExternalId(externalId);
    public void SetExchange(string exchange) => Exchange = exchange;
    public void ConfigureExchange(string exchange, string externalId)
    {
        AccountType = EAccountType.Exchange;
        Exchange = exchange;
        ExternalId = NormalizeExternalId(externalId);
    }
    public void Select() => IsSelected = true;
    public void Deselect() => IsSelected = false;
    public void ToggleEnabled() => Enabled = !Enabled;
    public void Enable() => Enabled = true;
    public void Disable() => Enabled = false;
    public void SoftDelete() => IsDeleted = true;
    public void SetInitialBalance(decimal balance)
    {
        if (Balance == 0 && balance > 0)
            Balance = balance;
    }
    public decimal TotalDeposited() => _accountTransactions.Sum(x => x.TransactionType switch
    {
        EAccountTransactionType.DepositFiat => x.Amount,
        EAccountTransactionType.TransferIn => x.Amount,
        EAccountTransactionType.WithdrawToBank => -x.Amount,
        EAccountTransactionType.TransferOut => -x.Amount,
        _ => 0
    });
    public IReadOnlyCollection<AccountTransaction> AccountTransactions => _accountTransactions.AsReadOnly();
    internal void AddTransaction(AccountTransaction accountTransaction)
    {
        _accountTransactions.Add(accountTransaction);
    }
    internal void SubtractFromBalance(decimal balance)
    {
        Balance -= balance;
    }
    internal void AddToBalance(decimal balance)
    {
        Balance += balance;
    }

    public Response AddCryptoAsset(CryptoAsset cryptoAsset)
    {
        var cryptoAssetExists = _cryptoAssets.Any(x => x.CoinMarketCapId == cryptoAsset.CoinMarketCapId);
        if (cryptoAssetExists)
            return new Response("Crypto asset already exists", false);

        _cryptoAssets.Add(cryptoAsset);
        return new Response("", true);
    }

    private static string? NormalizeExternalId(string? externalId) => string.IsNullOrWhiteSpace(externalId) ? null : externalId;
}
