using api.AzureKeyVault;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;
using Xunit;
using FluentAssertions;

namespace unit_tests.ExchangesTests.Commands;

public class SaveBybitIntegrationCredentialsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateIntegrationAndWriteCanonicalSecrets()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DataContext(options);
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        var handler = new SaveBybitIntegrationCredentialsCommandHandler(CredentialService(context, keyVault.Object));

        var result = await handler.Handle(new SaveBybitIntegrationCredentialsCommand(1, "api-key", "api-secret", BybitRegion.Eu), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var integration = await context.ExchangeIntegrations.SingleAsync();
        integration.Exchange.Should().Be("Bybit");
        integration.Status.Should().Be("Configured");
        integration.Enabled.Should().BeTrue();
        integration.Region.Should().Be("Eu");
        keyVault.Verify(x => x.SetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(1, "api-key"), "api-key"), Times.Once);
        keyVault.Verify(x => x.SetSecretAsync(BybitCredentialKeys.LegacyIntegrationKey(1, "api-secret"), "api-secret"), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_WhenWriteFails_ShouldReturnErrorWithoutCreatingIntegration(int failingCall)
    {
        var options = new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new DataContext(options);
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        var calls = 0;
        keyVault.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, _) => ++calls == failingCall ? Task.FromException(new Exception("failed")) : Task.CompletedTask);
        var handler = new SaveBybitIntegrationCredentialsCommandHandler(CredentialService(context, keyVault.Object));

        var result = await handler.Handle(new SaveBybitIntegrationCredentialsCommand(1, "key", "secret", BybitRegion.Global), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("recovery may be required");
        (await context.ExchangeIntegrations.CountAsync()).Should().Be(0);
    }

    private static IBybitCredentialSetService CredentialService(DataContext context, IKeyVaultService vault) =>
        new BybitCredentialSetService(context, vault, Mock.Of<ILogger<BybitCredentialSetService>>());
}
