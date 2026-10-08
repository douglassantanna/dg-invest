using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;

namespace unit_tests.ExchangesTests.Commands;

public class ResumeBybitIntegrationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenConnectionTestSucceeds_ResumesAutoPausedIntegration()
    {
        using var context = CreateContext();
        var integration = AutoPausedIntegration();
        context.ExchangeIntegrations.Add(integration);
        await context.SaveChangesAsync();
        var keyVault = CredentialsVault();
        var bybit = new Mock<IBybitService>();
        bybit.Setup(service => service.TestConnectionAsync("api-key", "api-secret", BybitRegion.Global))
            .ReturnsAsync(true);
        var handler = new ResumeBybitIntegrationCommandHandler(context, keyVault.Object, bybit.Object);

        var result = await handler.Handle(new ResumeBybitIntegrationCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        integration.Enabled.Should().BeTrue();
        integration.Status.Should().Be("Configured");
        integration.ConsecutiveTransportFailures.Should().Be(0);
        integration.AutoPausedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenConnectionTestFails_LeavesIntegrationPaused()
    {
        using var context = CreateContext();
        var integration = AutoPausedIntegration();
        context.ExchangeIntegrations.Add(integration);
        await context.SaveChangesAsync();
        var keyVault = CredentialsVault();
        var bybit = new Mock<IBybitService>();
        bybit.Setup(service => service.TestConnectionAsync("api-key", "api-secret", BybitRegion.Global))
            .ReturnsAsync(false);
        var handler = new ResumeBybitIntegrationCommandHandler(context, keyVault.Object, bybit.Object);

        var result = await handler.Handle(new ResumeBybitIntegrationCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        integration.Enabled.Should().BeFalse();
        integration.Status.Should().Be(ExchangeIntegration.AutoPausedStatus);
    }

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DataContext(options);
    }

    private static ExchangeIntegration AutoPausedIntegration()
    {
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.RecordPermanentCredentialFailure("10003", "Invalid API key", "/v5/user/submembers", null, DateTime.UtcNow);
        return integration;
    }

    private static Mock<IKeyVaultService> CredentialsVault()
    {
        var vault = new Mock<IKeyVaultService>();
        vault.Setup(service => service.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(1, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-key"));
        vault.Setup(service => service.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(1, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-secret"));
        return vault;
    }
}
