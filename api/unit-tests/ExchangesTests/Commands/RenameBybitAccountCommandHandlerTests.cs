using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;

namespace unit_tests.ExchangesTests.Commands;

public class RenameBybitAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenBybitExchangeAccountExists_ShouldUpdateNickname()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DataContext(options);
        var account = new Account("Original name", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var handler = new RenameBybitAccountCommandHandler(context);

        var result = await handler.Handle(new RenameBybitAccountCommand(1, account.Id, "Trading bot"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await context.Accounts.FindAsync(account.Id))!.Name.Should().Be("Trading bot");
    }

    [Fact]
    public async Task Handle_WhenAccountIsManual_ShouldRejectRename()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DataContext(options);
        var account = new Account("Manual", 1);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var handler = new RenameBybitAccountCommandHandler(context);

        var result = await handler.Handle(new RenameBybitAccountCommand(1, account.Id, "Trading bot"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(400);
    }
}
