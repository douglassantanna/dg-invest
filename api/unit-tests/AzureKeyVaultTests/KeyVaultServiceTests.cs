using Azure;
using Azure.Security.KeyVault.Secrets;
using api.AzureKeyVault;
using Microsoft.Extensions.Logging;
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
}
