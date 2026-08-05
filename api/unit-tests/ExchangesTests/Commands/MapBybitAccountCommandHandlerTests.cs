using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;
using Microsoft.Extensions.Logging;

namespace unit_tests.ExchangesTests.Commands;

public class MapBybitAccountCommandHandlerTests
{
    private readonly DataContext _context;
    private readonly MapBybitAccountCommandHandler _handler;

    public MapBybitAccountCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        var logger = Mock.Of<ILogger<MapBybitAccountCommandHandler>>();
        _handler = new MapBybitAccountCommandHandler(_context, logger);
    }

    [Fact]
    public async Task Handle_WhenAccountExists_ShouldMapUidAndReturnSuccess()
    {
        var account = new Account("sub1", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var cmd = new MapBybitAccountCommand(UserId: 1, AccountId: account.Id, ExternalId: "UID-001");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("UID-001");

        var saved = await _context.Accounts.FindAsync(account.Id);
        saved!.ExternalId.Should().Be("UID-001");
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturnNotFound()
    {
        var cmd = new MapBybitAccountCommand(UserId: 1, AccountId: 999, ExternalId: "UID-001");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenAccountBelongsToDifferentUser_ShouldReturnNotFound()
    {
        var account = new Account("sub1", 1);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var cmd = new MapBybitAccountCommand(UserId: 2, AccountId: account.Id, ExternalId: "UID-001");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenUidAlreadyMappedToDifferentAccount_ShouldReturnConflict()
    {
        var account1 = new Account("sub1", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var account2 = new Account("sub2", 1);
        _context.Accounts.AddRange(account1, account2);
        await _context.SaveChangesAsync();

        var cmd = new MapBybitAccountCommand(UserId: 1, AccountId: account2.Id, ExternalId: "UID-001");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(400);
        result.Message.Should().Contain("already mapped");
    }

    [Fact]
    public async Task Handle_WhenUidIsMappedByDifferentUser_ShouldAllowMapping()
    {
        var firstUserAccount = new Account("sub1", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var secondUserAccount = new Account("sub2", 2);
        _context.Accounts.AddRange(firstUserAccount, secondUserAccount);
        await _context.SaveChangesAsync();

        var command = new MapBybitAccountCommand(UserId: 2, AccountId: secondUserAccount.Id, ExternalId: "UID-001");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await _context.Accounts.FindAsync(secondUserAccount.Id);
        saved!.ExternalId.Should().Be("UID-001");
        saved.Exchange.Should().Be("Bybit");
    }

    [Fact]
    public async Task Handle_SameAccountCanRemaSameUid()
    {
        var account = new Account("sub1", 1);
        account.SetExternalId("UID-001");
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var cmd = new MapBybitAccountCommand(UserId: 1, AccountId: account.Id, ExternalId: "UID-001");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidInput_ShouldReturnValidationErrors()
    {
        var cmd = new MapBybitAccountCommand(UserId: 0, AccountId: 0, ExternalId: "");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Validation failed");
        result.Data.Should().BeOfType<List<string>>();
    }
}
