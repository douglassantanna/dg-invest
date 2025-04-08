using api.Cache;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Data;
using api.Services.Contracts;
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
    public async Task Handle_UserDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);
        _mockTimeframeCalculator.Setup(c => c.CalculateStartTime(ETimeframe._24h)).Returns(1000);

        _mockCacheService.Setup(c => c.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<Result<List<MarketDataPointDto>>>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MarketDataPointDto>>.Failure("User not found."));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found.");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidUserWithNoData_ReturnsEmptyGroupedData()
    {
        // Arrange
        var request = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);

        _mockCacheService.Setup(x => x.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<IEnumerable<object>>>>(),
            TimeSpan.FromMinutes(1),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((string key, Func<CancellationToken, Task<IEnumerable<object>>> factory, TimeSpan _, CancellationToken ct) =>
            factory(ct).Result);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidUserWithData_ReturnsGroupedData()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var query = new GetMarketDataByTimeframeQuery(user.Id, ETimeframe._24h);
        _mockCacheService.Setup(x => x.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<IEnumerable<object>>>>(),
            TimeSpan.FromMinutes(1),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((string key, Func<CancellationToken, Task<IEnumerable<object>>> factory, TimeSpan _, CancellationToken ct) =>
            factory(ct).Result);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        var groupedData = result.Cast<MarketDataPointDto>().ToList();
        groupedData.Should().HaveCount(2); // Grouped by hour
        groupedData.Should().Contain(x => x.Time == (now - 7200) / 3600 * 3600 && x.Value == 100);
        groupedData.Should().Contain(x => x.Time == (now - 3600) / 3600 * 3600 && x.Value == 200);
    }
}