using api.Cache;
using api.Cryptos.Models;
using api.Services.Contracts;
using api.Shared;
using api.Users.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var now_aligned = (now / 3600) * 3600;
        const long oneMonthInterval = 30 * 86400;
        var now_aligned_month = (now_aligned / oneMonthInterval) * oneMonthInterval;

        var startTime = request.Timeframe switch
        {
            ETimeframe._24h => now_aligned - 86400,
            ETimeframe._7d => (now_aligned - 604800) / 86400 * 86400,  // Align to day boundary
            ETimeframe._1m => (now_aligned - 2592000) / 86400 * 86400,  // Align to day boundary
            ETimeframe._1y => now_aligned_month - (12 * oneMonthInterval),  // Align to month boundary
            ETimeframe.All => 0,
            _ => throw new ArgumentOutOfRangeException("time frame not supported")
        };

        var cacheKey = CacheKeyConstants.GenerateMarketDataCacheKey(request, startTime);

        // var cachedResults = await _cacheService.GetOrCreateAsync(cacheKey,
        // async (ct) =>
        // {
        var userResult = await _userRepository.GetByIdAsync(request.UserId, x => x.Include(u => u.Accounts));
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
        List<MarketDataPointDto> groupedData;

        if (request.Timeframe == ETimeframe._24h)
        {
            groupedData = snapshots
                .Where(x => x.Time >= startTime && x.Time <= now_aligned)
                .GroupBy(x =>
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(x.Time).UtcDateTime;
                    // Start of the hour
                    return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
                })
                .SelectMany(group =>
                {
                    var hourStartTimestamp = new DateTimeOffset(group.Key).ToUnixTimeSeconds();
                    return group
                        .OrderBy(x => x.Time)
                        .Select(x => new MarketDataPointDto(hourStartTimestamp, x.Value));
                })
                .OrderBy(x => x.Time)
                .ToList();
        }
        else if (request.Timeframe == ETimeframe._7d)
        {
            const long oneDayInterval = 86400;
            groupedData = new List<MarketDataPointDto>();
            for (var time = startTime; time < now_aligned; time += oneDayInterval)
            {
                var bucketStart = (time / oneDayInterval) * oneDayInterval;
                var bucketEnd = bucketStart + oneDayInterval;
                var lastSnapshot = snapshots
                    .Where(s => s.Time >= bucketStart && s.Time < bucketEnd)
                    .OrderByDescending(s => s.Time)
                    .FirstOrDefault();
                if (lastSnapshot != null)
                {
                    groupedData.Add(new MarketDataPointDto(bucketStart, lastSnapshot.Value));
                }
            }
        }
        else if (request.Timeframe == ETimeframe._1m)
        {
            const long oneDayInterval = 86400;
            groupedData = new List<MarketDataPointDto>();
            for (var time = startTime; time < now_aligned; time += oneDayInterval)
            {
                var bucketStart = (time / oneDayInterval) * oneDayInterval;
                var bucketEnd = bucketStart + oneDayInterval;
                var lastSnapshot = snapshots
                    .Where(s => s.Time >= bucketStart && s.Time < bucketEnd)
                    .OrderByDescending(s => s.Time)
                    .FirstOrDefault();
                if (lastSnapshot != null)
                {
                    groupedData.Add(new MarketDataPointDto(bucketStart, lastSnapshot.Value));
                }
            }
        }
        else if (request.Timeframe == ETimeframe._1y)
        {
            groupedData = new List<MarketDataPointDto>();
            for (var time = startTime; time < now_aligned_month; time += oneMonthInterval)
            {
                var bucketStart = (time / oneMonthInterval) * oneMonthInterval;
                var bucketEnd = bucketStart + oneMonthInterval;
                var lastSnapshot = snapshots
                    .Where(s => s.Time >= bucketStart && s.Time < bucketEnd)
                    .OrderByDescending(s => s.Time)
                    .FirstOrDefault();
                if (lastSnapshot != null)
                {
                    groupedData.Add(new MarketDataPointDto(bucketStart, lastSnapshot.Value));
                }
            }
        }
        else // ETimeframe.All
        {
            groupedData = snapshots
                .Where(x => x.Time >= startTime && x.Time <= now_aligned)
                .GroupBy(x =>
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(x.Time).UtcDateTime;
                    // Start of the month
                    return new DateTime(date.Year, date.Month, 1);
                })
                .SelectMany(group =>
                {
                    var monthStartTimestamp = new DateTimeOffset(group.Key).ToUnixTimeSeconds();
                    return group
                        .OrderBy(x => x.Time)
                        .Select(x => new MarketDataPointDto(monthStartTimestamp, x.Value));
                })
                .OrderBy(x => x.Time)
                .ToList();
        }

        return Result<IEnumerable<MarketDataPointDto>>.Success(groupedData);
        // },
        // absoluteExpiration,
        // cancellationToken);

        // return cachedResults;
    }
}
