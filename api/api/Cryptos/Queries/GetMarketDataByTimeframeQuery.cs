using api.Cryptos.Models;
using api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetMarketDataByTimeframeQuery(int UserId, ETimeframe Timeframe) : IRequest<IEnumerable<MarketDataPoint>>;
public class GetMarketDataByTimeframeQueryHandler : IRequestHandler<GetMarketDataByTimeframeQuery, IEnumerable<MarketDataPoint>>
{
    private readonly DataContext _dbContext;
    public GetMarketDataByTimeframeQueryHandler(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<MarketDataPoint>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
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
        var marketData = await _dbContext.MarketDataPoint
            .Where(m => m.UserId == request.UserId && m.Time >= startTime)
            .ToListAsync(cancellationToken);

        if (marketData == null || !marketData.Any())
        {
            throw new KeyNotFoundException("No market data found for the selected timeframe.");
        }

        return marketData;
    }
}
