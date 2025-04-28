using api.Cache;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Services.Contracts;
using api.Shared;
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
    public async Task When24hRequestedAndSnapshotsExist_ShouldReturnHourlyGroupedDataWithOriginalValues()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var startTime = now - 86400;
        var snapshots = GenerateSnapshotsFor24h(startTime, 1); // 2 snapshots/hour
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
            It.IsAny<int>(), It.IsAny<int>(), startTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, $"Result should be successful. Error: {result.Error}");
        Assert.NotNull(result.Value);
        var data = result.Value.ToList();
        Assert.Equal(expectedData.Count, data.Count);
        Assert.Equal(24, data.Count);
    }

    [Fact]
    public async Task When7dRequestedAndSnapshotsExist_ShouldReturnLastSnapshotPerDay()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._7d);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var startTime = now - (7 * 86400);

        var snapshots = GenerateSnapshotsFor7d(startTime, 24);
        Assert.NotNull(snapshots);
        Assert.NotEmpty(snapshots);
        Assert.Equal(7 * 24, snapshots.Count);

        var expectedData = new List<MarketDataPointDto>();
        const long oneDayInterval = 86400;
        for (var time = startTime; time < now; time += oneDayInterval)
        {
            var bucketStart = (time / oneDayInterval) * oneDayInterval;
            var bucketEnd = bucketStart + oneDayInterval;
            var lastSnapshot = snapshots
                .Where(s => s.Time >= bucketStart && s.Time < bucketEnd)
                .OrderByDescending(s => s.Time)
                .FirstOrDefault();
            if (lastSnapshot != null)
            {
                expectedData.Add(new MarketDataPointDto(bucketStart, lastSnapshot.Value));
            }
        }

        _mockUserRepository.Setup(r => r.GetByIdAsync(
            1,
            It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>>()))
            .ReturnsAsync(Result<User?>.Success(user));
        _mockUserPortfolioSnapshotsRepository.Setup(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var data = result.Value.ToList();
        Assert.Equal(expectedData.Count, data.Count);
        Assert.Equal(7, data.Count);
        _mockUserPortfolioSnapshotsRepository.Verify(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task When1mRequestedAndSnapshotsExist_ShouldReturnLastSnapshotPerDayFor30Days()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._1m);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var startTime = now - (30 * 86400);

        var snapshots = GenerateSnapshotsFor1m(startTime, 24);
        Assert.NotNull(snapshots);
        Assert.NotEmpty(snapshots);
        Assert.Equal(30 * 24, snapshots.Count);

        var expectedData = new List<MarketDataPointDto>();
        const long oneDayInterval = 86400;
        for (var time = startTime; time < now; time += oneDayInterval)
        {
            var bucketStart = (time / oneDayInterval) * oneDayInterval;
            var bucketEnd = bucketStart + oneDayInterval;
            var lastSnapshot = snapshots
                .Where(s => s.Time >= bucketStart && s.Time < bucketEnd)
                .OrderByDescending(s => s.Time)
                .FirstOrDefault();
            if (lastSnapshot != null)
            {
                expectedData.Add(new MarketDataPointDto(bucketStart, lastSnapshot.Value));
            }
        }

        _mockUserRepository.Setup(r => r.GetByIdAsync(
            1,
            It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>>()))
            .ReturnsAsync(Result<User?>.Success(user));
        _mockUserPortfolioSnapshotsRepository.Setup(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var data = result.Value.ToList();
        Assert.Equal(expectedData.Count, data.Count);
        Assert.Equal(30, data.Count);
        _mockUserPortfolioSnapshotsRepository.Verify(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task When1yRequestedAndSnapshotsExist_ShouldReturnLastSnapshotPerMonthFor12Months()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._1y);
        var user = new User("Douglas", "douglas@gmail.com", "12345678", Role.User);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var startTime = now - (365 * 86400);

        var snapshots = GenerateSnapshotsFor1y(startTime, 24);
        Assert.NotNull(snapshots);
        Assert.NotEmpty(snapshots);
        Assert.Equal(365 * 24, snapshots.Count);

        var expectedData = new List<MarketDataPointDto>();
        const long oneMonthInterval = 30 * 86400;
        for (var time = now - (12 * oneMonthInterval); time < now; time += oneMonthInterval)
        {
            var bucketStart = (time / oneMonthInterval) * oneMonthInterval;
            var bucketEnd = bucketStart + oneMonthInterval;
            var lastSnapshot = snapshots
                .Where(s => s.Time >= bucketStart && s.Time < bucketEnd)
                .OrderByDescending(s => s.Time)
                .FirstOrDefault();
            if (lastSnapshot != null)
            {
                expectedData.Add(new MarketDataPointDto(bucketStart, lastSnapshot.Value));
            }
        }

        _mockUserRepository.Setup(r => r.GetByIdAsync(
            1,
            It.IsAny<Func<IQueryable<User>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<User, object>>>()))
            .ReturnsAsync(Result<User?>.Success(user));
        _mockUserPortfolioSnapshotsRepository.Setup(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserPortfolioSnapshot>>.Success(snapshots));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var data = result.Value.ToList();
        Assert.Equal(expectedData.Count, data.Count);
        Assert.Equal(12, data.Count);
        _mockUserPortfolioSnapshotsRepository.Verify(r => r.GetPortfolioSnapshotsByUserIdAndAccountIdAndTimeFrameAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Once());
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
                var time = dayStart.AddMinutes(i * (1440.0 / snapshotsPerDay)).ToUnixTimeSeconds();
                var value = 118704.00208221m + (day * 1000) + (i * 10);
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
                var time = dayStart.AddMinutes(i * (1440.0 / snapshotsPerDay)).ToUnixTimeSeconds();
                var value = 118704.00208221m + (day * 1000) + (i * 10);
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

    private static List<UserPortfolioSnapshot> GenerateSnapshotsFor1y(long startTime, int snapshotsPerDay)
    {
        var snapshots = new List<UserPortfolioSnapshot>();
        var random = new Random();
        for (int day = 0; day < 365; day++)
        {
            var dayStart = DateTimeOffset.FromUnixTimeSeconds(startTime).AddDays(day);
            for (int i = 0; i < snapshotsPerDay; i++)
            {
                var time = dayStart.AddMinutes(i * (1440.0 / snapshotsPerDay)).ToUnixTimeSeconds();
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