using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace unit_tests.ExchangesTests.Commands;

public class TestBybitConnectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenConnectionValidationFails_ShouldNotMarkAccountVerified()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var syncStatus = new SyncStatus(1, account.Id, "Bybit");
        context.SyncStatuses.Add(syncStatus);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretAsync(It.IsAny<string>())).ReturnsAsync("configured");
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.TestConnectionAsync("configured", "configured")).ReturnsAsync(false);
        var logger = Mock.Of<ILogger<TestBybitConnectionCommandHandler>>();
        var handler = new TestBybitConnectionCommandHandler(keyVault.Object, bybitService.Object, context, logger);

        var result = await handler.Handle(new TestBybitConnectionCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        syncStatus.LastVerifiedAt.Should().BeNull();
    }
}
