using api.Cryptos.Models;
using api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetMarketDataByTimeframeQuery(int UserId, ETimeframe Timeframe) : IRequest<IEnumerable<object>>;
public class GetMarketDataByTimeframeQueryHandler : IRequestHandler<GetMarketDataByTimeframeQuery, IEnumerable<object>>
{
    private readonly DataContext _dbContext;
    public GetMarketDataByTimeframeQueryHandler(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<object>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long startTime;

        switch (request.Timeframe)
        {
            case ETimeframe._24h:
                startTime = now - (24 * 60 * 60); // 24 hours ago
                break;
            case ETimeframe._7d:
                startTime = now - (7 * 24 * 60 * 60); // 7 days ago
                break;
            case ETimeframe._1m:
                startTime = now - (30 * 24 * 60 * 60); // 1 month ago (approx 30 days)
                break;
            case ETimeframe._1y:
                startTime = now - (365 * 24 * 60 * 60); // 1 year ago (approx 365 days)
                break;
            case ETimeframe.All:
                startTime = 0; // No filter, retrieve all data
                break;
            default:
                throw new ArgumentException("Invalid timeframe");
        }

        // Fetch the market data based on the timeframe
        // need to use accountId to retrieve only market data from selected account
        // since the line chart will display how the portfolio account is performing,
        // we need to sum up how much is the market value in the last timeframe,
        // so we need to sum up all the records from selected timeframe(lets say 24h) and accountId
        // and create a object like { time: 00000 (which will represent an hour, like 01:00), value: 1000 (the total of the values sum for that specific hour)}
        // and do this for the last 24 hours (or 7d,1m and so on)
        var marketData = await _dbContext.MarketDataPoint
            .Where(m => m.UserId == request.UserId && m.Time >= startTime)
            .Where(x => x.AccountId == 2007)
            .ToListAsync(cancellationToken);

        if (marketData == null || !marketData.Any())
        {
            throw new KeyNotFoundException("No market data found for the selected timeframe.");
        }

        var groupedData = marketData
           .GroupBy(m => (m.Time / 3600) * 3600) // Group by hour (time / 3600 will get us the hour)
           .Select(group => new
           {
               time = group.Key, // Unix timestamp for the hour
               value = group.Sum(m => m.CoinPrice) // Sum the market values for that hour
           })
           .ToList();

        return groupedData;
    }
}
