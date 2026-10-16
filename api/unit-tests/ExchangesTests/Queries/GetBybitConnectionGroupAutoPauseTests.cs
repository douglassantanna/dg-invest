using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Models;
using api.Exchanges.Queries;
using api.Exchanges.Services;

namespace unit_tests.ExchangesTests.Queries;

public class GetBybitConnectionGroupAutoPauseTests
{
    [Fact]
    public async Task Handle_WhenIntegrationIsAutoPaused_ReturnsPausedStateAndAccountsForReview()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new DataContext(options);
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.RecordPermanentCredentialFailure("10003", "Invalid API key", "/v5/order/history", 27, DateTime.UtcNow);
        var account = new Account("Bybit account", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.AddRange(integration, account);
        await context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(service => service.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "configured-value"));
        var handler = new GetBybitConnectionGroupQueryHandler(keyVault.Object, context);

        var response = await handler.Handle(new GetBybitConnectionGroupQuery(1), CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        var groups = response.Data.Should().BeOfType<List<BybitConnectionGroupDto>>().Subject;
        var group = groups.Should().ContainSingle().Subject;
        group.IntegrationStatus.Should().Be(ExchangeIntegration.AutoPausedStatus);
        group.IntegrationEnabled.Should().BeFalse();
        group.LastErrorCode.Should().Be("10003");
        group.LastErrorEndpoint.Should().Be("/v5/order/history");
        group.Subaccounts.Should().ContainSingle().Which.AccountId.Should().Be(account.Id);
        keyVault.Verify(service => service.GetSecretReadResultAsync(It.IsAny<string>()), Times.Never);
    }
}
