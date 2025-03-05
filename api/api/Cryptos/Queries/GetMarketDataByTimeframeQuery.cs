using api.Cache;
using api.Cryptos.Models;
using api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace api.Cryptos.Queries;
public record GetMarketDataByTimeframeQuery(int UserId, ETimeframe Timeframe) : IRequest<IEnumerable<object>>;
public class GetMarketDataByTimeframeQueryHandler : IRequestHandler<GetMarketDataByTimeframeQuery, IEnumerable<object>>
{
    private readonly DataContext _context;
    private readonly ICacheService _cache;
    public GetMarketDataByTimeframeQueryHandler(DataContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IEnumerable<object>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
    {
        // var absoluteExpiration = TimeSpan.FromMinutes(1);
        // var cacheKey = CacheKeyConstants.MarketData + request.UserId + "_" + request.Timeframe;
        // var cacheKey = CacheKeyConstants.GenerateMarketDataCacheKey(request);
        long startTime = CalculateStartTime(request.Timeframe);

        var user = await _context.Users
                                 .AsNoTracking()
                                 .Include(x => x.Accounts)
                                 .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user == null)
            return Enumerable.Empty<object>();

        var userAccount = user.Accounts.FirstOrDefault(x => x.IsSelected)!;
        var marketData = await _context.UserPortfolioSnapshots
            .AsNoTracking()
            .Where(m => m.UserId == request.UserId && m.Time >= startTime)
            .Where(x => x.AccountId == userAccount.Id)
            .ToListAsync(cancellationToken);

        var groupedData = marketData
            .GroupBy(m => (m.Time / 3600) * 3600)
            .Select(group => new
            {
                time = group.Key,
                value = group.Sum(m => m.Value)
            })
            .ToList();

        return groupedData;
    }

    private static long CalculateStartTime(ETimeframe timeframe)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return timeframe switch
        {
            ETimeframe._24h => now - (24 * 60 * 60),
            ETimeframe._7d => now - (7 * 24 * 60 * 60),
            ETimeframe._1m => now - (30 * 24 * 60 * 60),
            ETimeframe._1y => now - (365 * 24 * 60 * 60),
            ETimeframe.All => 0,
            _ => throw new ArgumentException("Invalid timeframe")
        };
    }
}
