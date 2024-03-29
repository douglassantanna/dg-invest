using System.Text.Json.Serialization;
using api.Models.Cryptos;
using api.Shared;

namespace api.Users.Models;
public class User : Entity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool EmailConfirmed { get; set; } = false;
    private readonly List<CryptoAsset> _criptoAssets = new();

    public User(string fullName,
                string email,
                string password,
                Role role)
    {
        FullName = fullName;
        Email = email;
        Password = password;
        Role = role;
    }

    public IReadOnlyCollection<CryptoAsset> CryptoAssets => _criptoAssets;

    internal void AddCryptoAsset(CryptoAsset cryptoAsset)
    {
        var cryptoAssetExists = _criptoAssets.Any(x => x.CoinMarketCapId == cryptoAsset.CoinMarketCapId);
        if (cryptoAssetExists)
        {
            throw new Exception("Crypto asset already exists");
        }
        _criptoAssets.Add(cryptoAsset);
    }
}