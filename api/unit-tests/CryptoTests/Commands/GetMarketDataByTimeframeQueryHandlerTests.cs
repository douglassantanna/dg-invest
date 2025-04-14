using api.Cache;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Data;
using api.Services.Contracts;
using api.Shared;
using api.unit_tests.Helpers;
using api.Users.Models;
using api.Users.Repositories;
using FluentAssertions;
using Moq;

namespace unit_tests.CryptoTests.Commands;
public class GetMarketDataByTimeframeQueryHandlerTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserPortfolioSnapshotsRepository> _mockUserPortfolioSnapshotsRepository;
    private readonly GetMarketDataByTimeframeQueryHandler _handler;
    private readonly Mock<ITimeframeCalculator> _mockTimeframeCalculator;

    public GetMarketDataByTimeframeQueryHandlerTests()
    {
        _mockTimeframeCalculator = new Mock<ITimeframeCalculator>();
        _mockCacheService = new Mock<ICacheService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserPortfolioSnapshotsRepository = new Mock<IUserPortfolioSnapshotsRepository>();
        _handler = new GetMarketDataByTimeframeQueryHandler(
            _mockCacheService.Object,
            _mockUserRepository.Object,
            _mockUserPortfolioSnapshotsRepository.Object,
            _mockTimeframeCalculator.Object);
    }

    [Fact]
    public async Task WhenUserAndSnapshotsExist_ShouldReturnGroupedData()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var snapshots = new List<UserPortfolioSnapshot>()
        {
            new( ){Time = 1696118400, Value= 100m}, // Oct 1, 2023 00:00 UTC
            new(){Time= 1696204800, Value= 200m }  // Oct 2, 2023 00:00 UTC
        };

        _mockTimeframeCalculator.Setup(t => t.CalculateStartTime(ETimeframe._24h)).Returns(1696032000); // Sep 30, 2023

        _mockUserRepository.Setup(r => r.GetByIdAsync(1, null)).ReturnsAsync(Result<User>.Success(user));

        _mockUserPortfolioSnapshotsRepository.Setup(x => x.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            1,
            10,
            1696032000,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));

        _mockTimeframeCalculator.Setup(t => t.CalculateGroupingInterval(ETimeframe._24h)).Returns(86400); // 24 hours in seconds
        _mockCacheService.Setup(c => c.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<Result<IEnumerable<MarketDataPointDto>>>>>(),
            TimeSpan.FromMinutes(1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<MarketDataPointDto>>.Success(new List<MarketDataPointDto>
            {
                new(1696118400, 100m),
                new(1696204800, 200m)
            }));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var data = result.Value.ToList();
        Assert.Equal(2, data.Count);
        Assert.Equal(1696118400, data[0].Time);
        Assert.Equal(100m, data[0].Value);
        Assert.Equal(1696204800, data[1].Time);
        Assert.Equal(200m, data[1].Value);
    }

    [Fact]
    public async Task When24hRequestedAndSnapshotsExist_ShouldReturn24HourlyDataPoints()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var snapshots = MarketDataHelper.GenerateDailySnapshots();
        Assert.NotNull(snapshots);
        Assert.NotEmpty(snapshots);
        var startTime = snapshots.Last().Time - 24 * 3600; // Last 24 hours from latest snapshot

        // Generate expected data for 24 hours
        var expectedData = new List<MarketDataPointDto>();
        for (int i = 0; i < 24; i++)
        {
            var time = startTime + i * 3600;
            var snapshotsInHour = snapshots.Where(s => s.Time >= time && s.Time < time + 3600).ToList();
            var value = snapshotsInHour.Any() ? snapshotsInHour.Sum(s => s.Value) : 0m;
            expectedData.Add(new MarketDataPointDto(time, value));
        }

        _mockTimeframeCalculator.Setup(t => t.CalculateStartTime(ETimeframe._24h)).Returns(startTime);
        _mockTimeframeCalculator.Setup(t => t.CalculateGroupingInterval(ETimeframe._24h)).Returns(3600);
        _mockUserRepository.Setup(r => r.GetByIdAsync(1, null)).ReturnsAsync(Result<User>.Success(user));
        _mockUserPortfolioSnapshotsRepository.Setup(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            1, 10, startTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));
        _mockCacheService.Setup(c => c.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<Result<IEnumerable<MarketDataPointDto>>>>>(),
            TimeSpan.FromMinutes(1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<MarketDataPointDto>>.Success(expectedData));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var data = result.Value.ToList();
        Assert.Equal(24, data.Count);
        for (int i = 0; i < 24; i++)
        {
            Assert.Equal(expectedData[i].Time, data[i].Time);
            Assert.Equal(expectedData[i].Value, data[i].Value);
        }
    }
}