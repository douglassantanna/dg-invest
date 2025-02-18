using api.CoinMarketCap.Service;
using api.Data;
using api.Models.Cryptos;
using api.Services;
using api.Users.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using unit_tests.Extensions;

public class MarketDataServiceTests
{
  private readonly Mock<ILogger<MarketDataService>> _mockLogger;
  private readonly DataContext _context;
  private readonly Mock<ICoinMarketCapService> _mockCoinMarketCapService;
  private readonly MarketDataService _sut;

  public MarketDataServiceTests()
  {
    _mockLogger = new Mock<ILogger<MarketDataService>>();

    var options = new DbContextOptionsBuilder<DataContext>()
        .UseSqlite("DataSource=:memory:")
        .Options;

    _context = new DataContext(options);
    _context.Database.OpenConnection();
    _context.Database.EnsureCreated();

    _mockCoinMarketCapService = new Mock<ICoinMarketCapService>();
    _sut = new MarketDataService(_mockLogger.Object, _context, _mockCoinMarketCapService.Object);
  }

  [Fact]
  public async Task FetchAndProcessMarketDataAsync_ShouldReturnEarly_WhenNoUsersExist()
  {
    // Arrange
    // No users added to the context

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    _mockCoinMarketCapService.Verify(x => x.GetQuotesByIds(It.IsAny<string[]>()), Times.Never);
  }

  [Fact]
  public async Task FetchAndProcessMarketDataAsync_ShouldReturnEarly_WhenNoCryptoAssetsExist()
  {
    // Arrange
    var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    _mockCoinMarketCapService.Verify(x => x.GetQuotesByIds(It.IsAny<string[]>()), Times.Never);
  }

  [Fact]
  public async Task FetchAndProcessMarketDataAsync_ShouldProcessMarketData_WhenCryptoAssetsExist()
  {
    // Arrange
    var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
    var account = user.Accounts.First();
    account.AddCryptoAsset(new CryptoAsset("BTC", "USD", "Bitcoin", 1));
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    var marketData = MockDbSetExtensions.CreateFakeGetQuoteResponse();
    _mockCoinMarketCapService.Setup(x => x.GetQuotesByIds(It.IsAny<string[]>()))
        .ReturnsAsync(marketData);

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    _mockCoinMarketCapService.Verify(x => x.GetQuotesByIds(It.IsAny<string[]>()), Times.Once);
    _context.MarketDataPoint.Should().NotBeEmpty();
  }

  [Fact]
  public async Task FetchAndProcessMarketDataAsync_ShouldSaveMarketDataPoints()
  {
    // Arrange
    var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
    var account = user.Accounts.First();
    account.AddCryptoAsset(new CryptoAsset("Bitcoin", "USD", "BTC", 1));
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    var marketData = MockDbSetExtensions.CreateFakeGetQuoteResponse();
    _mockCoinMarketCapService.Setup(x => x.GetQuotesByIds(It.IsAny<string[]>()))
        .ReturnsAsync(marketData);

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    _context.MarketDataPoint.Should().NotBeEmpty();
    var marketDataPoint = _context.MarketDataPoint.First();
    marketDataPoint.UserId.Should().Be(user.Id);
    marketDataPoint.AccountId.Should().Be(account.Id);
    marketDataPoint.CoinSymbol.Should().Be("BTC");
  }
}
