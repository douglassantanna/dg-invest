using api.Shared;

namespace api.Cryptos.Models;
public class Crypto : Entity
{
    public Crypto(string name, string symbol, string image, int coinMarketCapId)
    {
        Name = name;
        Symbol = symbol;
        Image = image;
        CoinMarketCapId = coinMarketCapId;
    }

    public string Name { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public string Image { get; private set; } = string.Empty;
    public int CoinMarketCapId { get; private set; }
}
