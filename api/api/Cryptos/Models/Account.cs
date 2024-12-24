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
    public string SubaccountTag { get; private set; } = string.Empty;
    public Account(string subaccountTag, int userId)
    {
        SubaccountTag = subaccountTag;
        UserId = userId;
        IsSelected = true;
    }
    public void Select() => IsSelected = true;
    public void Deselect() => IsSelected = false;
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
}
