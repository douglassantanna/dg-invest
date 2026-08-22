using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Commands;
using api.Exchanges.Services;
using Microsoft.Extensions.Logging;

namespace unit_tests.ExchangesTests.Commands;

public class SaveBybitIntegrationCredentialsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateIntegrationAndActivateImmutableUserScopedSet()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DataContext(options);
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        var handler = new SaveBybitIntegrationCredentialsCommandHandler(
            keyVault.Object, context, Mock.Of<ILogger<SaveBybitIntegrationCredentialsCommandHandler>>());

        var result = await handler.Handle(new SaveBybitIntegrationCredentialsCommand(1, "api-key", "api-secret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integration = await context.ExchangeIntegrations.SingleAsync();
        integration.Exchange.Should().Be("Bybit");
        integration.Status.Should().Be("Configured");
        integration.ActiveCredentialSetId.Should().NotBeNull();
        keyVault.Verify(x => x.SetSecretAsync(BybitCredentialKeys.SetKey(integration.ActiveCredentialSetId!, "api-key"), "api-key"), Times.Once);
        keyVault.Verify(x => x.SetSecretAsync(BybitCredentialKeys.SetKey(integration.ActiveCredentialSetId!, "api-secret"), "api-secret"), Times.Once);
        (await context.CredentialUpdateOperations.SingleAsync()).State.Should().Be("Active");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_WhenWriteFails_ShouldRecordRecoveryWithoutChangingActiveIntegration(int failingCall)
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DataContext(options);
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        var calls = 0;
        keyVault.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, _) => ++calls == failingCall ? Task.FromException(new Exception("failed")) : Task.CompletedTask);
        var handler = new SaveBybitIntegrationCredentialsCommandHandler(keyVault.Object, context, Mock.Of<ILogger<SaveBybitIntegrationCredentialsCommandHandler>>());

        var result = await handler.Handle(new SaveBybitIntegrationCredentialsCommand(1, "key", "secret"), CancellationToken.None);

        result.Data.Should().Be(500);
        keyVault.Verify(x => x.SetSecretAsync(It.Is<string>(key => key.StartsWith("bybit-integration-")), It.IsAny<string>()), Times.Never);
        (await context.ExchangeIntegrations.CountAsync()).Should().Be(0);
        (await context.CredentialUpdateOperations.SingleAsync()).State.Should().Be("RecoveryRequired");
    }

    [Fact]
    public async Task Handle_WhenPriorReadIsUnavailable_ShouldReturn503WithoutWritesOrIntegration()
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DataContext(options);
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));
        var handler = new SaveBybitIntegrationCredentialsCommandHandler(keyVault.Object, context, Mock.Of<ILogger<SaveBybitIntegrationCredentialsCommandHandler>>());

        var result = await handler.Handle(new SaveBybitIntegrationCredentialsCommand(1, "key", "secret"), CancellationToken.None);

        result.Data.Should().Be(503);
        keyVault.Verify(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        (await context.ExchangeIntegrations.CountAsync()).Should().Be(0);
        (await context.CredentialUpdateOperations.SingleAsync()).State.Should().Be("RecoveryRequired");
    }
}
