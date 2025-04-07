using api.Cache;
using api.Cryptos.Models;
using api.Services.Contracts;
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
    private readonly ITimeframeCalculator _timeframeCalculator;
    public GetMarketDataByTimeframeQueryHandler(
        ICacheService cacheService,
        IUserRepository userRepository,
        IUserPortfolioSnapshotsRepository userPortfolioSnapshotsRepository,
        ITimeframeCalculator timeframeCalculator)
    {
        _cacheService = cacheService;
        _userRepository = userRepository;
        _userPortfolioSnapshotsRepository = userPortfolioSnapshotsRepository;
        _timeframeCalculator = timeframeCalculator;
    }

    public async Task<IEnumerable<MarketDataPointDto>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
    {
        var absoluteExpiration = TimeSpan.FromMinutes(1);
        long startTime = _timeframeCalculator.CalculateStartTime(request.Timeframe);
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

            var snapshots = (List<UserPortfolioSnapshotDto>)snapshotResult.Data!;
            var groupedData = GroupSnapshots(snapshots, request.Timeframe);

            return groupedData;
        },
        absoluteExpiration,
        cancellationToken);

        return cachedResults;
    }
    private List<MarketDataPointDto> GroupSnapshots(List<UserPortfolioSnapshotDto> snapshots, ETimeframe timeframe)
    {
        var interval = _timeframeCalculator.CalculateGroupingInterval(timeframe);

        return snapshots
            .GroupBy(s => (s.Time / 3600) * 3600) // Group by hour initially
            .Select(g => new { Time = g.Key, Value = g.Sum(s => s.Value) })
            .GroupBy(h => (h.Time / interval) * interval) // Group by timeframe interval
            .Select(g => new MarketDataPointDto(g.Key, g.Sum(h => h.Value)))
            .ToList();
    }
}
