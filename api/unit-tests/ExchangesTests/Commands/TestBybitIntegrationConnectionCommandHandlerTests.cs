using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;

namespace unit_tests.ExchangesTests.Commands;

public class TestBybitIntegrationConnectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenIntegrationCredentialsAreValid_TestsIntegrationKeyAndRegion()
    {
        using var context = CreateContext();
        var integration = new ExchangeIntegration(7, "Bybit");
        integration.SetRegion("Eu");
        integration.MarkConfigured();
        integration.MarkEnabled();
        context.ExchangeIntegrations.Add(integration);
        await context.SaveChangesAsync();
        var keyVault = IntegrationCredentialVault(7);
        var bybit = new Mock<IBybitService>();
        bybit.Setup(service => service.TestConnectionAsync("integration-key", "integration-secret", BybitRegion.Eu))
            .ReturnsAsync(true);
        var handler = new TestBybitIntegrationConnectionCommandHandler(context, keyVault.Object, bybit.Object);

        var response = await handler.Handle(new TestBybitIntegrationConnectionCommand(7), CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        bybit.Verify(service => service.TestConnectionAsync("integration-key", "integration-secret", BybitRegion.Eu), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenKeyVaultIsUnavailable_Returns503WithoutCallingBybit()
    {
        using var context = CreateContext();
        context.ExchangeIntegrations.Add(new ExchangeIntegration(7, "Bybit"));
        await context.SaveChangesAsync();
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(service => service.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));
        var bybit = new Mock<IBybitService>();
        var handler = new TestBybitIntegrationConnectionCommandHandler(context, keyVault.Object, bybit.Object);

        var response = await handler.Handle(new TestBybitIntegrationConnectionCommand(7), CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        response.Data.Should().Be(503);
        bybit.Verify(service => service.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBybitRejectsIntegrationCredentials_ReturnsTheBybitError()
    {
        using var context = CreateContext();
        context.ExchangeIntegrations.Add(new ExchangeIntegration(7, "Bybit"));
        await context.SaveChangesAsync();
        var keyVault = IntegrationCredentialVault(7);
        var bybit = new Mock<IBybitService>();
        bybit.Setup(service => service.TestConnectionAsync("integration-key", "integration-secret", It.IsAny<BybitRegion>()))
            .ThrowsAsync(new BybitApiException(10003, "API key is invalid"));
        var handler = new TestBybitIntegrationConnectionCommandHandler(context, keyVault.Object, bybit.Object);

        var response = await handler.Handle(new TestBybitIntegrationConnectionCommand(7), CancellationToken.None);

        response.IsSuccess.Should().BeFalse();
        response.Data.Should().Be(400);
        response.Message.Should().Contain("10003 - API key is invalid");
    }

    private static Mock<IKeyVaultService> IntegrationCredentialVault(int userId)
    {
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(service => service.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "integration-key"));
        keyVault.Setup(service => service.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(userId, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "integration-secret"));
        return keyVault;
    }

    private static DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DataContext(options);
    }
}
