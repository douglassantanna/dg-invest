using api.Cache;
using api.Cryptos.Models;
using api.Cryptos.Queries;
using api.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace unit_tests.CryptoTests.Commands;
public class GetMarketDataByTimeframeQueryHandlerTests
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

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenUserNotFound()
    {
        // Arrange
        var request = new GetMarketDataByTimeframeQuery(1, ETimeframe._24h);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}