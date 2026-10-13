using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;
using api.Users.Models;

namespace unit_tests.ExchangesTests.Commands;

public class SyncBybitAccountsMasterReconnectTests
{
    [Fact]
    public async Task Handle_WhenMasterAccountIsDisabledAndDiscoverySucceeds_ReenablesExistingMaster()
    {
        using var context = CreateContext();
        var (user, integration, masterAccount) = await AddDisconnectedMasterAsync(context);
        integration.MarkConfigured();
        integration.MarkEnabled();
        await context.SaveChangesAsync();
        var handler = CreateHandler(context, new Mock<IBybitService>(), user.Id);
        Mock.Get(handler.BybitService).Setup(service => service.GetSubAccountsAsync(
                "integration-key", "integration-secret", It.IsAny<BybitRegion>()))
            .ReturnsAsync([]);

        var result = await handler.CommandHandler.Handle(new SyncBybitAccountsCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await context.Accounts.SingleAsync(account => account.Id == masterAccount.Id)).Enabled.Should().BeTrue();
        integration.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMasterAccountIsDisabledAndDiscoveryFails_LeavesMasterDisabled()
    {
        using var context = CreateContext();
        var (user, integration, masterAccount) = await AddDisconnectedMasterAsync(context);
        integration.MarkConfigured();
        integration.MarkEnabled();
        await context.SaveChangesAsync();
        var handler = CreateHandler(context, new Mock<IBybitService>(), user.Id);
        Mock.Get(handler.BybitService).Setup(service => service.GetSubAccountsAsync(
                "integration-key", "integration-secret", It.IsAny<BybitRegion>()))
            .ThrowsAsync(new BybitApiException(10003, "invalid key"));

        var result = await handler.CommandHandler.Handle(new SyncBybitAccountsCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await context.Accounts.SingleAsync(account => account.Id == masterAccount.Id)).Enabled.Should().BeFalse();
        integration.Enabled.Should().BeTrue();
    }

    private static async Task<(User User, ExchangeIntegration Integration, Account MasterAccount)> AddDisconnectedMasterAsync(DataContext context)
    {
        var user = new User("Master User", "master@example.com", "hash", Role.User);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var integration = new ExchangeIntegration(user.Id, "Bybit");
        var masterAccount = new Account("Bybit master account", user.Id, EAccountType.Exchange, "Bybit", "50001");
        context.Accounts.Add(masterAccount);
        await context.SaveChangesAsync();

        integration.LinkMasterAccount(masterAccount.Id);
        integration.MarkDisconnected();
        masterAccount.Disable();
        context.SyncStatuses.Add(new SyncStatus(user.Id, masterAccount.Id, "Bybit"));
        context.ExchangeIntegrations.Add(integration);
        await context.SaveChangesAsync();
        return (user, integration, masterAccount);
    }

    private static TestHandler CreateHandler(DataContext context, Mock<IBybitService> bybitService, int userId)
    {
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(service => service.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "integration-key"));
        keyVault.Setup(service => service.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "integration-secret"));
        var orderSync = new Mock<IBybitOrderSyncService>();
        orderSync.Setup(service => service.ProcessOpeningBalanceAsync(It.IsAny<Account>(), userId, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var commandHandler = new SyncBybitAccountsCommandHandler(
            bybitService.Object,
            keyVault.Object,
            context,
            orderSync.Object,
            Mock.Of<ILogger<SyncBybitAccountsCommandHandler>>());
        return new TestHandler(bybitService.Object, commandHandler);
    }

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DataContext(options);
    }

    private sealed record TestHandler(IBybitService BybitService, SyncBybitAccountsCommandHandler CommandHandler);
}
