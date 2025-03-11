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
    private readonly ICacheService _cacheService;
    public GetMarketDataByTimeframeQueryHandler(DataContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<object>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
    {
        var absoluteExpiration = TimeSpan.FromMinutes(1);
        long startTime = CalculateStartTime(request.Timeframe);
        var cacheKey = CacheKeyConstants.GenerateMarketDataCacheKey(request, startTime);

        var cachedResults = await _cacheService.GetOrCreateAsync(cacheKey,
        async (ct) =>
        {
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

            var interval = CalculateGroupingInterval(request.Timeframe);
            var groupedData = marketData
                            .GroupBy(m => (m.Time / interval) * interval)
                            .Select(group => new
                            {
                                time = group.Key,
                                value = group.Sum(m => m.Value)
                            })
                            .ToList();

            return groupedData;
        },
        absoluteExpiration,
        cancellationToken);

        return cachedResults;
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

    private static long CalculateGroupingInterval(ETimeframe timeframe)
    {
        return timeframe switch
        {
            ETimeframe._24h => 3600, // Group by hour
            ETimeframe._7d => 86400, // Group by day
            ETimeframe._1m => 86400, // Group by day
            ETimeframe._1y => 2592000, // Group by month (approx 30 days)
            ETimeframe.All => 31536000, // Group by year (approx 365 days)
            _ => throw new ArgumentException("Invalid timeframe")
        };
    }
}
