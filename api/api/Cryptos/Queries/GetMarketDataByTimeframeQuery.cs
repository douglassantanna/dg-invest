using api.Cache;
using api.Cryptos.Models;
using api.Users.Repositories;
using MediatR;

namespace api.Cryptos.Queries;
public record GetMarketDataByTimeframeQuery(int UserId, ETimeframe Timeframe) : IRequest<IEnumerable<MarketDataPointDto>>;
public record MarketDataPointDto(long Time, decimal Value);
public class GetMarketDataByTimeframeQueryHandler : IRequestHandler<GetMarketDataByTimeframeQuery, IEnumerable<MarketDataPointDto>>
{
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly IUserPortfolioSnapshotsRepository _userPortfolioSnapshotsRepository;
    public GetMarketDataByTimeframeQueryHandler(
        ICacheService cacheService,
        IUserRepository userRepository,
        IUserPortfolioSnapshotsRepository userPortfolioSnapshotsRepository)
    {
        _cacheService = cacheService;
        _userRepository = userRepository;
        _userPortfolioSnapshotsRepository = userPortfolioSnapshotsRepository;
    }

    public async Task<IEnumerable<MarketDataPointDto>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
    {
        var absoluteExpiration = TimeSpan.FromMinutes(1);
        long startTime = CalculateStartTime(request.Timeframe);
        var cacheKey = CacheKeyConstants.GenerateMarketDataCacheKey(request, startTime);

        var cachedResults = await _cacheService.GetOrCreateAsync(cacheKey,
        async (ct) =>
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                return [];

            var selectedAccount = user.Accounts.FirstOrDefault(x => x.IsSelected);
            if (selectedAccount == null)
                return Enumerable.Empty<MarketDataPointDto>();

            var snapshotResult = await _userPortfolioSnapshotsRepository.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
                request.UserId,
                selectedAccount.Id,
                startTime,
                cancellationToken
            );
            if (!snapshotResult.IsSuccess)
                return [];

            var interval = CalculateGroupingInterval(request.Timeframe);
            var snapshots = (List<UserPortfolioSnapshotDto>)snapshotResult.Data!;

            var hourlyData = snapshots
                            .GroupBy(m => (m.Time / 3600) * 3600)
                            .Select(group => new
                            {
                                time = group.Key,
                                value = group.Sum(m => m.Value)
                            })
                            .ToList();

            var groupedData = hourlyData
                            .GroupBy(h => (h.time / interval) * interval)
                            .Select(group => new MarketDataPointDto(
                                group.Key,
                                group.Sum(h => h.value)
                            ))
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
