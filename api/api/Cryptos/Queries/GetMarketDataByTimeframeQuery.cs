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
        var startTime = request.Timeframe switch
        {
            ETimeframe._24h => now - 86400,
            ETimeframe._7d => now - 604800,
            ETimeframe._1m => now - 2592000,
            ETimeframe._1y => now - 31104000,
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

        if (request.Timeframe == ETimeframe._1y)
        {
            groupedData = snapshots
                          .Where(x => x.Time >= startTime && x.Time <= now)
                          .GroupBy(x => DateTimeOffset.FromUnixTimeSeconds(x.Time).UtcDateTime.Date)
                          .Select(group =>
                          {
                              var date = group.Key;
                              var timestamp = new DateTimeOffset(date).ToUnixTimeSeconds();
                              return new MarketDataPointDto(timestamp, group.Sum(y => y.Value));
                          })
                          .OrderBy(x => x.Time)
                          .ToList();
        }

        groupedData = snapshots
                      .Where(x => x.Time >= startTime && x.Time <= now)
                      .Select(x => new MarketDataPointDto(x.Time, x.Value))
                      .ToList();

        return Result<IEnumerable<MarketDataPointDto>>.Success(groupedData);
        // },
        // absoluteExpiration,
        // cancellationToken);

        // return cachedResults;
    }
}
