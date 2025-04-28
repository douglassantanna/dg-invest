using api.Shared;

namespace api.Cryptos.Models;

public class MarketDataPoint : Entity
{
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public string CoinSymbol { get; set; } = string.Empty;
    public decimal CoinPrice { get; set; }
    public long Time { get; set; }
    protected MarketDataPoint()
    {

    }
    public MarketDataPoint(int userId, int accountId, string coinSymbol, decimal coinPrice, long time)
    {
        UserId = userId;
        AccountId = accountId;
        CoinSymbol = coinSymbol;
        CoinPrice = coinPrice;
        Time = time;
    }
}

public class UserPortfolioSnapshot : Entity
{
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public decimal Value { get; set; }
    public long Time { get; set; }
    public UserPortfolioSnapshot()
    {

    }
}
