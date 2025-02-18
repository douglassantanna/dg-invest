using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using api.Services;
using api.Users.Models;
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
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    _context = new DataContext(options);
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

}
