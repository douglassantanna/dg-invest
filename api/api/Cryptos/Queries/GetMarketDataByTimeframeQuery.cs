using api.Cache;
using api.Cryptos.Models;
using api.Services.Contracts;
using api.Shared;
using api.Users.Repositories;
using MediatR;

namespace api.Cryptos.Queries;
public record GetMarketDataByTimeframeQuery(int UserId, ETimeframe Timeframe) : IRequest<Result<IEnumerable<MarketDataPointDto>>>;
public record MarketDataPointDto(long Time, decimal Value);
public class GetMarketDataByTimeframeQueryHandler : IRequestHandler<GetMarketDataByTimeframeQuery, Result<IEnumerable<MarketDataPointDto>>>
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

    public async Task<Result<IEnumerable<MarketDataPointDto>>> Handle(GetMarketDataByTimeframeQuery request, CancellationToken cancellationToken)
    {
        var absoluteExpiration = TimeSpan.FromMinutes(1);
        // long startTime = _timeframeCalculator.CalculateStartTime(request.Timeframe);
        var startTime = timeframe switch
        {
            ETimeframe._24h => now - 86400,
            ETimeframe._7d => now - 604800,
            ETimeframe._1m => now - 2592000,
            ETimeframe._1y => now - 31104000,
            _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, null)
        };

        var cacheKey = CacheKeyConstants.GenerateMarketDataCacheKey(request, startTime);

        var cachedResults = await _cacheService.GetOrCreateAsync(cacheKey,
        async (ct) =>
        {
            var userResult = await _userRepository.GetByIdAsync(request.UserId);
            if (!userResult.IsSuccess)
                return Result<IEnumerable<MarketDataPointDto>>.Failure($"User not found: {userResult.Error}");

            var selectedAccount = userResult?.Value?.Accounts.FirstOrDefault(x => x.IsSelected);
            if (selectedAccount == null)
                return Result<IEnumerable<MarketDataPointDto>>.Failure("No selected account found for user");

            var snapshotResult = await _userPortfolioSnapshotsRepository.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
                request.UserId,
                selectedAccount.Id,
                startTime,
                cancellationToken
            );
            if (!snapshotResult.IsSuccess)
                return Result<IEnumerable<MarketDataPointDto>>.Failure($"Failed to fetch snapshots: {snapshotResult.Error}");

            var snapshots = snapshotResult.Value!;
            var groupedData = GroupSnapshots(snapshots, request.Timeframe);

            return Result<IEnumerable<MarketDataPointDto>>.Success(groupedData);
        },
        absoluteExpiration,
        cancellationToken);

        return cachedResults;
    }
    private List<MarketDataPointDto> GroupSnapshots(List<UserPortfolioSnapshot> snapshots, ETimeframe timeframe)
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
