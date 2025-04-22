using api.Cache;
using api.Cryptos.Models;
using api.Cryptos.Queries;
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
    public async Task WhenUserAndSnapshotsExist_ShouldReturnEmptyData()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var snapshots = MarketDataHelper.GenerateDailySnapshots();
        var startTime = snapshots.Last().Time - 24 * 3600; // Last 24 hours from latest snapshot

        _mockTimeframeCalculator.Setup(t => t.CalculateStartTime(ETimeframe._24h)).Returns(startTime);
        _mockTimeframeCalculator.Setup(t => t.CalculateGroupingInterval(ETimeframe._24h)).Returns(3600); // Hourly for _24h
        _mockUserRepository.Setup(r => r.GetByIdAsync(1, null)).ReturnsAsync(Result<User>.Success(user));
        _mockUserPortfolioSnapshotsRepository.Setup(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            1, 10, startTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));
        _mockCacheService.Setup(c => c.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<Result<IEnumerable<MarketDataPointDto>>>>>(),
            TimeSpan.FromMinutes(1),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<MarketDataPointDto>>.Success([]));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var data = result.Value.ToList();
        Assert.Empty(data);
    }

    [Fact]
    public async Task When24hRequestedAndSnapshotsExist_ShouldReturnHourlyGroupedDataWithOriginalValues()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var startTime = now - 86400;
        var snapshots = GenerateSnapshotsFor24h(startTime, 2); // 2 snapshots/hour
        Assert.NotNull(snapshots);
        Assert.NotEmpty(snapshots);

        var expectedData = new List<MarketDataPointDto>();
        const long oneHourInterval = 3600;
        for (var time = startTime; time < now; time += oneHourInterval)
        {
            var bucketStart = (time / oneHourInterval) * oneHourInterval;
            var bucketEnd = bucketStart + oneHourInterval;
            var snapshotsInBucket = snapshots
                .Where(s => s.Time >= bucketStart && s.Time < bucketEnd)
                .OrderBy(s => s.Time);
            expectedData.AddRange(snapshotsInBucket.Select(s => new MarketDataPointDto(bucketStart, s.Value)));
        }

        _mockUserRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>>()))
            .ReturnsAsync(Result<User?>.Success(user));
        _mockUserPortfolioSnapshotsRepository.Setup(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            1, 0, startTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, $"Result should be successful. Error: {result.Error}");
        Assert.NotNull(result.Value);
        var data = result.Value.ToList();
        Assert.Equal(expectedData.Count, data.Count);
        for (int i = 0; i < expectedData.Count; i++)
        {
            Assert.Equal(expectedData[i].Time, data[i].Time);
            Assert.Equal(expectedData[i].Value, data[i].Value);
        }
    }
    private static List<UserPortfolioSnapshot> GenerateSnapshotsFor24h(long startTime, int snapshotsPerHour)
    {
        var snapshots = new List<UserPortfolioSnapshot>();
        var random = new Random();
        for (int hour = 0; hour < 24; hour++)
        {
            var hourStart = DateTimeOffset.FromUnixTimeSeconds(startTime).AddHours(hour);
            for (int i = 0; i < snapshotsPerHour; i++)
            {
                var time = hourStart.AddMinutes(random.Next(0, 60)).ToUnixTimeSeconds();
                var value = 118704.00208221m + (hour * 100) + (i * 10);
                snapshots.Add(new UserPortfolioSnapshot
                {
                    UserId = 1,
                    AccountId = 10,
                    Time = time,
                    Value = value
                });
            }
        }
        return snapshots;
    }

    private static List<UserPortfolioSnapshot> GenerateSnapshotsFor7d(long startTime, int snapshotsPerDay)
    {
        var snapshots = new List<UserPortfolioSnapshot>();
        var random = new Random();
        for (int day = 0; day < 7; day++)
        {
            var dayStart = DateTimeOffset.FromUnixTimeSeconds(startTime).AddDays(day);
            for (int i = 0; i < snapshotsPerDay; i++)
            {
                var time = dayStart.AddHours(i * (24.0 / snapshotsPerDay)).ToUnixTimeSeconds(); // Spread snapshots
                var value = 118704.00208221m + (day * 100) + (i * 10);
                snapshots.Add(new UserPortfolioSnapshot
                {
                    UserId = 1,
                    AccountId = 10,
                    Time = time,
                    Value = value
                });
            }
        }
        return snapshots;
    }

    private static List<UserPortfolioSnapshot> GenerateSnapshotsFor1m(long startTime, int snapshotsPerDay)
    {
        var snapshots = new List<UserPortfolioSnapshot>();
        var random = new Random();
        for (int day = 0; day < 30; day++)
        {
            var dayStart = DateTimeOffset.FromUnixTimeSeconds(startTime).AddDays(day);
            for (int i = 0; i < snapshotsPerDay; i++)
            {
                var time = dayStart.AddHours(random.Next(0, 24)).ToUnixTimeSeconds();
                var value = 118704.00208221m + (day * 100) + (i * 10);
                snapshots.Add(new UserPortfolioSnapshot
                {
                    UserId = 1,
                    AccountId = 10,
                    Time = time,
                    Value = value
                });
            }
        }
        return snapshots;
    }

    private static List<UserPortfolioSnapshot> GenerateSnapshotsFor1y(long startTime, int snapshotsPerWeek)
    {
        var snapshots = new List<UserPortfolioSnapshot>();
        var random = new Random();
        for (int week = 0; week < 52; week++)
        {
            var weekStart = DateTimeOffset.FromUnixTimeSeconds(startTime).AddDays(week * 7);
            for (int i = 0; i < snapshotsPerWeek; i++)
            {
                var time = weekStart.AddDays(random.Next(0, 7)).AddHours(random.Next(0, 24)).ToUnixTimeSeconds();
                var value = 118704.00208221m + (week * 100) + (i * 10);
                snapshots.Add(new UserPortfolioSnapshot
                {
                    UserId = 1,
                    AccountId = 10,
                    Time = time,
                    Value = value
                });
            }
        }
        return snapshots;
    }

    private static List<UserPortfolioSnapshot> GenerateSnapshotsForAll(long startTime, int snapshotsPerMonth)
    {
        var snapshots = new List<UserPortfolioSnapshot>();
        var random = new Random();
        for (int month = 0; month < 60; month++)
        {
            var monthStart = DateTimeOffset.FromUnixTimeSeconds(startTime).AddMonths(month);
            for (int i = 0; i < snapshotsPerMonth; i++)
            {
                var time = monthStart.AddDays(random.Next(0, 28)).AddHours(random.Next(0, 24)).ToUnixTimeSeconds();
                var value = 118704.00208221m + (month * 100) + (i * 10);
                snapshots.Add(new UserPortfolioSnapshot
                {
                    UserId = 1,
                    AccountId = 10,
                    Time = time,
                    Value = value
                });
            }
        }
        return snapshots;
    }

}