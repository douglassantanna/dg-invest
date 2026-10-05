using Azure;
using Azure.Security.KeyVault.Secrets;
using api.AzureKeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace unit_tests.AzureKeyVaultTests;

public class KeyVaultServiceTests
{
    [Fact]
    public async Task GetSecretReadResultAsync_WhenSecretDoesNotExist_ReturnsNotFound()
    {
        var client = new Mock<SecretClient>();
        client.Setup(x => x.GetSecretAsync("missing", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret was not found", "SecretNotFound", null));
        var service = new KeyVaultService(client.Object, Mock.Of<ILogger<KeyVaultService>>());

        var result = await service.GetSecretReadResultAsync("missing");

        result.Status.Should().Be(KeyVaultSecretReadStatus.NotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetSecretReadResultAsync_WhenKeyVaultRejectsRequest_ReturnsUnavailable()
    {
        var client = new Mock<SecretClient>();
        client.Setup(x => x.GetSecretAsync("restricted", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(403, "Forbidden", "Forbidden", null));
        var service = new KeyVaultService(client.Object, Mock.Of<ILogger<KeyVaultService>>());

        var result = await service.GetSecretReadResultAsync("restricted");

        result.Status.Should().Be(KeyVaultSecretReadStatus.Unavailable);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetSecretReadResultAsync_When404IsNotSecretNotFound_ReturnsUnavailable()
    {
        var client = new Mock<SecretClient>();
        client.Setup(x => x.GetSecretAsync("unexpected-404", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Unexpected endpoint", "Unknown", null));
        var service = new KeyVaultService(client.Object, Mock.Of<ILogger<KeyVaultService>>());

        var result = await service.GetSecretReadResultAsync("unexpected-404");

        result.Status.Should().Be(KeyVaultSecretReadStatus.Unavailable);
    }

    [Fact]
    public async Task GetSecretReadResultAsync_WhenSecretIsBybitScoped_UsesBybitVaultClient()
    {
        var platformClient = new Mock<SecretClient>();
        var bybitClient = new Mock<SecretClient>();
        bybitClient.Setup(x => x.GetSecretAsync("bybit-test-key", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret was not found", "SecretNotFound", null));
        var service = new KeyVaultService(platformClient.Object, bybitClient.Object, Mock.Of<ILogger<KeyVaultService>>());

        var result = await service.GetSecretReadResultAsync("bybit-test-key");

        result.Status.Should().Be(KeyVaultSecretReadStatus.NotFound);
        bybitClient.Verify(x => x.GetSecretAsync("bybit-test-key", null, It.IsAny<CancellationToken>()), Times.Once);
        platformClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Constructor_WhenBybitVaultUriIsMissing_UsesPlatformVaultUri()
    {
        var service = new KeyVaultService(
            Options.Create(new KeyVaultSettings { VaultUri = "https://platform.vault.azure.net" }),
            Options.Create(new BybitKeyVaultSettings()),
            Mock.Of<ILogger<KeyVaultService>>());

        service.Should().NotBeNull();
    }
}
