using api.Cache;
using api.Cryptos.Commands;
using api.Cryptos.Models;
using api.Data;
using api.Users.Models;
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

    [Fact]
    public async Task Handle_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new SelectAccountCommand(1, 1);
        var cancellationToken = new CancellationToken();

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task Handle_ShouldSelectAccount_WhenRequestIsValid()
    {
        // Arrange
        var request = new SelectAccountCommand(1, 1);
        var cancellationToken = new CancellationToken();

        var user = new User("test name", "emaill@test.com", "fakePassword", Role.Admin);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(request, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Account selected successfully");
        user.Accounts.First().IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAccountIsSelected()
    {
        // Arrange
        var user = new User("Test User", "test@test.com", "password", Role.User);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new SelectAccountCommand(UserId: user.Id, AccountId: user.Accounts.First().Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Account selected successfully");
        _mockCacheService.Verify(x => x.Remove(It.IsAny<string>()), Times.Exactly(3));
    }
}