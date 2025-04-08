using api.Cache;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Data;
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
    public async Task Handle_ReturnsGroupedData_WhenUserAndSnapshotsExist()
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

    // [Fact]
    // public async Task Handle_ValidUserWithNoData_ReturnsEmptyGroupedData()
    // {
    //     // Arrange
    //     var request = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);

    //     _mockCacheService.Setup(x => x.GetOrCreateAsync(
    //         It.IsAny<string>(),
    //         It.IsAny<Func<CancellationToken, Task<IEnumerable<object>>>>(),
    //         TimeSpan.FromMinutes(1),
    //         It.IsAny<CancellationToken>()
    //     )).ReturnsAsync((string key, Func<CancellationToken, Task<IEnumerable<object>>> factory, TimeSpan _, CancellationToken ct) =>
    //         factory(ct).Result);

    //     // Act
    //     var result = await _handler.Handle(request, CancellationToken.None);

    //     // Assert
    //     result.Should().BeEmpty();
    // }

    // [Fact]
    // public async Task Handle_ValidUserWithData_ReturnsGroupedData()
    // {
    //     // Arrange
    //     var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    //     var query = new GetMarketDataByTimeframeQuery(user.Id, ETimeframe._24h);
    //     _mockCacheService.Setup(x => x.GetOrCreateAsync(
    //         It.IsAny<string>(),
    //         It.IsAny<Func<CancellationToken, Task<IEnumerable<object>>>>(),
    //         TimeSpan.FromMinutes(1),
    //         It.IsAny<CancellationToken>()
    //     )).ReturnsAsync((string key, Func<CancellationToken, Task<IEnumerable<object>>> factory, TimeSpan _, CancellationToken ct) =>
    //         factory(ct).Result);

    //     // Act
    //     var result = await _handler.Handle(request, CancellationToken.None);

    //     // Assert
    //     var groupedData = result.Cast<MarketDataPointDto>().ToList();
    //     groupedData.Should().HaveCount(2); // Grouped by hour
    //     groupedData.Should().Contain(x => x.Time == (now - 7200) / 3600 * 3600 && x.Value == 100);
    //     groupedData.Should().Contain(x => x.Time == (now - 3600) / 3600 * 3600 && x.Value == 200);
    // }
}