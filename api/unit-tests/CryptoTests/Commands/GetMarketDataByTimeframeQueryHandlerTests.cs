using api.Cache;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Data;
using api.Users.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace unit_tests.CryptoTests.Commands;
public class GetMarketDataByTimeframeQueryHandlerTests : IDisposable
{
    private readonly DataContext _context;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly GetMarketDataByTimeframeQueryHandler _handler;

    public GetMarketDataByTimeframeQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new DataContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _mockCacheService = new Mock<ICacheService>();
        _handler = new GetMarketDataByTimeframeQueryHandler(_context, _mockCacheService.Object);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ReturnsEmptyEnumerable()
    {
        // Arrange
        var query = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        _mockCacheService.Setup(x => x.GetOrCreateAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<IEnumerable<object>>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(Enumerable.Empty<object>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidUserWithNoData_ReturnsEmptyGroupedData()
    {
        // Arrange
        var request = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);
        var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
        var account = user.Accounts.First();
        var marketData = new List<UserPortfolioSnapshot>
            {
                new UserPortfolioSnapshot { UserId = 1, AccountId = 1, Time = 3600, Value = 100 },
                new UserPortfolioSnapshot { UserId = 1, AccountId = 1, Time = 7200, Value = 200 },
                new UserPortfolioSnapshot { UserId = 1, AccountId = 1, Time = 10800, Value = 300 }
            };

        _context.Users.Add(user);
        _context.UserPortfolioSnapshots.AddRange(marketData);
        await _context.SaveChangesAsync();
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
}