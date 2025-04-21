using api.CoinMarketCap;
using api.CoinMarketCap.Service;
using api.Data;
using api.Models.Cryptos;
using api.Services;
using api.Users.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

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

  [Theory]
  [InlineData("BTC", 1, 100.0, 100.0)]  // Test case 1: 1 BTC * $100 = 100
  [InlineData("ETH", 2, 200.0, 400.0)]  // Test case 2: 2 ETH * $200 = 400
  [InlineData("XRP", 5, 1.0, 5.0)]      // Test case 3: 5 XRP * $1 = 5
  public async Task FetchAndProcessMarketDataAsync_ShouldCalculatePortfolioValue_Correctly_WithMultipleCoins(
        string assetType,
        int quantity,
        decimal pricePerCoin,
        decimal expectedPortfolioValue)
  {
    // Arrange
    var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
    var account = user.Accounts.First();

    account.AddCryptoAsset(new CryptoAsset(assetType, "USD", assetType, quantity));
    var cryptoAsset = account.CryptoAssets.First();
    cryptoAsset.AddBalance(quantity);

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    var coinPrices = new GetQuoteResponse(
        new Status(0, null),
        new Dictionary<string, Coin>
        {
                { "1", new Coin(1, "Bitcoin", "BTC", DateTime.UtcNow, new Quote(new USD(pricePerCoin, DateTime.UtcNow, 0))) },
                { "2", new Coin(2, "Ethereum", "ETH", DateTime.UtcNow, new Quote(new USD(pricePerCoin, DateTime.UtcNow, 0))) },
                { "3", new Coin(3, "Ripple", "XRP", DateTime.UtcNow, new Quote(new USD(pricePerCoin, DateTime.UtcNow, 0))) }
        }
    );

    _mockCoinMarketCapService.Setup(x => x.GetQuotesByIds(It.IsAny<string[]>()))
        .ReturnsAsync(coinPrices);

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    var calculatedPortfolioValue = quantity * pricePerCoin;
    calculatedPortfolioValue.Should().Be(expectedPortfolioValue);
  }

  [Fact]
  public async Task FetchAndProcessMarketDataAsync_ShouldNotProcess_WhenCoinMarketCapServiceReturnsNoData()
  {
    // Arrange
    var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
    var account = user.Accounts.First();
    account.AddCryptoAsset(new CryptoAsset("BTC", "USD", "Bitcoin", 1));
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    _mockCoinMarketCapService.Setup(x => x.GetQuotesByIds(It.IsAny<string[]>()))
        .ReturnsAsync(new GetQuoteResponse(new Status(0, null), new Dictionary<string, Coin>()));

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    _context.UserPortfolioSnapshots.Should().BeEmpty();
  }

  [Fact]
  public async Task FetchAndProcessMarketDataAsync_ShouldHandleException_WhenFetchingMarketDataFails()
  {
    // Arrange
    var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
    var account = user.Accounts.First();
    account.AddCryptoAsset(new CryptoAsset("BTC", "USD", "Bitcoin", 1));
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    _mockCoinMarketCapService.Setup(x => x.GetQuotesByIds(It.IsAny<string[]>()))
        .ThrowsAsync(new Exception("Market data fetch failed"));

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    _context.UserPortfolioSnapshots.Should().BeEmpty();
  }

  [Fact]
  public async Task FetchAndProcessMarketDataAsync_ShouldNotCreateSnapshot_WhenAssetBalanceIsZero()
  {
    // Arrange
    var user = new User("test name", "testEmail@test.com", "randonPassword", Role.Admin);
    var account = user.Accounts.First();
    account.AddCryptoAsset(new CryptoAsset("BTC", "USD", "Bitcoin", 1));
    var cryptoAsset = account.CryptoAssets.First();
    cryptoAsset.AddBalance(0);
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    // Act
    await _sut.FetchAndProcessMarketDataAsync(CancellationToken.None);

    // Assert
    _context.UserPortfolioSnapshots.Should().BeEmpty();
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
}
