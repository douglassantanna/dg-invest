using api.Cache;
using api.Cryptos.Commands;
using api.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace unit_tests.CryptoAccountTests.Commands;
public class SelectAccountCommandHandlerTests
{
    private readonly DataContext _context;
    private readonly Mock<ILogger<SelectAccountCommandHandler>> _mockLogger;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly SelectAccountCommandHandler _handler;

    public SelectAccountCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new DataContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
        _mockLogger = new Mock<ILogger<SelectAccountCommandHandler>>();
        _mockCacheService = new Mock<ICacheService>();
        _handler = new SelectAccountCommandHandler(_context, _mockLogger.Object, _mockCacheService.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailed_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new SelectAccountCommand(0, 0);
        var cancellationToken = new CancellationToken();

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Validation failed");
    }
}