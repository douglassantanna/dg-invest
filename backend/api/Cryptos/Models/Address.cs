using api.Models.Cryptos;
using api.Shared;

namespace api.Cryptos.Models;
public class Address : Entity
{
    public string AddressName { get; private set; }
    public string AddressValue { get; private set; }
    public CryptoAsset CryptoAsset { get; private set; }
    public int CryptoAssetId { get; private set; }
    public bool Deleted { get; private set; }
    public Address()
    {
        Deleted = false;
    }
}
