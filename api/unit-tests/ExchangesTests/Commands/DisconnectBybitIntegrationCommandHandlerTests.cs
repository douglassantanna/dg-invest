using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using Microsoft.Extensions.Logging;

namespace unit_tests.ExchangesTests.Commands;

public class DisconnectBybitIntegrationCommandHandlerTests
{
    private readonly DataContext _context;
    private readonly Mock<IKeyVaultService> _keyVault;
    private readonly DisconnectBybitIntegrationCommandHandler _handler;

    public DisconnectBybitIntegrationCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _keyVault = new Mock<IKeyVaultService>();
        _handler = new DisconnectBybitIntegrationCommandHandler(_keyVault.Object, _context, Mock.Of<ILogger<DisconnectBybitIntegrationCommandHandler>>());
    }

    [Fact]
    public async Task Handle_WhenIntegrationExists_ShouldDisableIntegrationAndStatusesWithoutDeletingAccounts()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.ActivateCredentialSet("integration-set");
        var account = new Account("Bybit account", 1, EAccountType.Exchange, "Bybit", "UID-001");
        _context.AddRange(integration, account);
        await _context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.ActivateCredentialSet("account-set");
        var activeIntegrationOperation = new CredentialUpdateOperation(1, "Bybit", null, null, null);
        activeIntegrationOperation.MarkActive();
        var activeAccountOperation = new CredentialUpdateOperation(1, "Bybit", account.Id, null, null);
        activeAccountOperation.MarkActive();
        var pendingOperation = new CredentialUpdateOperation(1, "Bybit", account.Id, null, null);
        _context.AddRange(status, activeIntegrationOperation, activeAccountOperation, pendingOperation);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new DisconnectBybitIntegrationCommand(1), CancellationToken.None);

        _context.ChangeTracker.Clear();
        integration = await _context.ExchangeIntegrations.SingleAsync(x => x.UserId == 1 && x.Exchange == "Bybit");
        account = await _context.Accounts.SingleAsync(x => x.Id == account.Id);
        status = await _context.SyncStatuses.SingleAsync(x => x.Id == status.Id);
        activeIntegrationOperation = await _context.CredentialUpdateOperations.SingleAsync(x => x.OperationId == activeIntegrationOperation.OperationId);
        activeAccountOperation = await _context.CredentialUpdateOperations.SingleAsync(x => x.OperationId == activeAccountOperation.OperationId);
        pendingOperation = await _context.CredentialUpdateOperations.SingleAsync(x => x.OperationId == pendingOperation.OperationId);

        result.IsSuccess.Should().BeTrue();
        integration.Enabled.Should().BeFalse();
        integration.Status.Should().Be("Disconnected");
        integration.ActiveCredentialSetId.Should().BeNull();
        account.IsDeleted.Should().BeFalse();
        status.IsEnabled.Should().BeFalse();
        status.Status.Should().Be("Disconnected");
        status.ActiveCredentialSetId.Should().BeNull();
        activeIntegrationOperation.State.Should().Be("Retired");
        activeAccountOperation.State.Should().Be("Retired");
        pendingOperation.State.Should().Be("Superseded");
        _keyVault.Verify(v => v.SetSecretAsync("bybit-integration-1-api-key", string.Empty), Times.Once);
        _keyVault.Verify(v => v.SetSecretAsync("bybit-integration-1-api-secret", string.Empty), Times.Once);
        _keyVault.Verify(v => v.SetSecretAsync($"bybit-1-{account.Id}-api-key", string.Empty), Times.Once);
        _keyVault.Verify(v => v.SetSecretAsync($"bybit-1-{account.Id}-api-secret", string.Empty), Times.Once);
        _keyVault.Verify(v => v.SetSecretAsync($"bybit-1-{account.Id}-webhook-secret", string.Empty), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCalledTwice_ShouldRemainSuccessful()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.ActivateCredentialSet("integration-set");
        _context.ExchangeIntegrations.Add(integration);
        await _context.SaveChangesAsync();

        var first = await _handler.Handle(new DisconnectBybitIntegrationCommand(1), CancellationToken.None);
        var second = await _handler.Handle(new DisconnectBybitIntegrationCommand(1), CancellationToken.None);

        _context.ChangeTracker.Clear();
        integration = await _context.ExchangeIntegrations.SingleAsync(x => x.UserId == 1 && x.Exchange == "Bybit");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        integration.Enabled.Should().BeFalse();
        integration.ActiveCredentialSetId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenLegacyCleanupFails_ShouldStillDisconnectIntegration()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.ActivateCredentialSet("integration-set");
        _context.ExchangeIntegrations.Add(integration);
        await _context.SaveChangesAsync();
        _keyVault.Setup(v => v.SetSecretAsync(It.IsAny<string>(), string.Empty)).ThrowsAsync(new InvalidOperationException("vault failed"));

        var result = await _handler.Handle(new DisconnectBybitIntegrationCommand(1), CancellationToken.None);

        _context.ChangeTracker.Clear();
        integration = await _context.ExchangeIntegrations.SingleAsync(x => x.UserId == 1 && x.Exchange == "Bybit");

        result.IsSuccess.Should().BeTrue();
        integration.Enabled.Should().BeFalse();
        integration.ActiveCredentialSetId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenIntegrationRowIsMissing_ShouldStillDisableStatusesAndBlankLegacySecrets()
    {
        var account = new Account("Bybit account", 1, EAccountType.Exchange, "Bybit", "UID-001");
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.ActivateCredentialSet("account-set");
        _context.SyncStatuses.Add(status);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new DisconnectBybitIntegrationCommand(1), CancellationToken.None);

        _context.ChangeTracker.Clear();
        account = await _context.Accounts.SingleAsync(x => x.Id == account.Id);
        status = await _context.SyncStatuses.SingleAsync(x => x.Id == status.Id);

        result.IsSuccess.Should().BeTrue();
        account.IsDeleted.Should().BeFalse();
        status.IsEnabled.Should().BeFalse();
        status.ActiveCredentialSetId.Should().BeNull();
        _keyVault.Verify(v => v.SetSecretAsync("bybit-integration-1-api-key", string.Empty), Times.Once);
        _keyVault.Verify(v => v.SetSecretAsync($"bybit-1-{account.Id}-webhook-secret", string.Empty), Times.Once);
    }
}
