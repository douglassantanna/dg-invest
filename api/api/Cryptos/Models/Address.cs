using api.Models.Cryptos;

namespace api.Cryptos.Models;
public class Address
{
    public int Id { get; private set; }
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
