using System.Text.Json.Serialization;
using api.Models.Cryptos;

namespace api.Users.Models;
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool EmailConfirmed { get; set; } = false;
    private readonly List<CryptoAsset> _criptoAssets = new();
    public IReadOnlyCollection<CryptoAsset> CryptoAssets => _criptoAssets;
    // public ICollection<CryptoAsset> CryptoAssets2 { get; set; } = new List<CryptoAsset>();
}