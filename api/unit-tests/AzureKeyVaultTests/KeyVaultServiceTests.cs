using Azure;
using Azure.Security.KeyVault.Secrets;
using api.AzureKeyVault;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AzureResponse = Azure.Response;

namespace unit_tests.AzureKeyVaultTests;

public class KeyVaultServiceTests
{
    [Fact]
    public async Task GetSecretReadResultAsync_WhenBybitSecretIsFound_CachesValueAcrossReads()
    {
        var client = new Mock<SecretClient>();
        var response = AzureResponse.FromValue(new KeyVaultSecret("bybit-1-2-api-key", "cached-key"), Mock.Of<AzureResponse>());
        client.Setup(x => x.GetSecretAsync("bybit-1-2-api-key", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KeyVaultService(client.Object, client.Object, cache, Mock.Of<ILogger<KeyVaultService>>());

        var firstRead = await service.GetSecretReadResultAsync("bybit-1-2-api-key");
        var secondRead = await service.GetSecretReadResultAsync("bybit-1-2-api-key");

        firstRead.Value.Should().Be("cached-key");
        secondRead.Value.Should().Be("cached-key");
        client.Verify(x => x.GetSecretAsync("bybit-1-2-api-key", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSecretAsync_InvalidatesCachedBybitSecret()
    {
        var client = new Mock<SecretClient>();
        var oldSecret = AzureResponse.FromValue(new KeyVaultSecret("bybit-1-2-api-key", "old-key"), Mock.Of<AzureResponse>());
        var newSecret = AzureResponse.FromValue(new KeyVaultSecret("bybit-1-2-api-key", "new-key"), Mock.Of<AzureResponse>());
        client.SetupSequence(x => x.GetSecretAsync("bybit-1-2-api-key", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldSecret)
            .ReturnsAsync(newSecret);
        client.Setup(x => x.SetSecretAsync("bybit-1-2-api-key", "new-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newSecret);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KeyVaultService(client.Object, client.Object, cache, Mock.Of<ILogger<KeyVaultService>>());

        (await service.GetSecretReadResultAsync("bybit-1-2-api-key")).Value.Should().Be("old-key");
        await service.SetSecretAsync("bybit-1-2-api-key", "new-key");
        var refreshed = await service.GetSecretReadResultAsync("bybit-1-2-api-key");

        refreshed.Value.Should().Be("new-key");
        client.Verify(x => x.GetSecretAsync("bybit-1-2-api-key", null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetSecretReadResultAsync_WhenBybitSecretIsUnavailable_DoesNotCacheFailure()
    {
        var client = new Mock<SecretClient>();
        var response = AzureResponse.FromValue(new KeyVaultSecret("bybit-1-2-api-key", "available-key"), Mock.Of<AzureResponse>());
        client.SetupSequence(x => x.GetSecretAsync("bybit-1-2-api-key", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "Unavailable"))
            .ReturnsAsync(response);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new KeyVaultService(client.Object, client.Object, cache, Mock.Of<ILogger<KeyVaultService>>());

        var unavailable = await service.GetSecretReadResultAsync("bybit-1-2-api-key");
        var recovered = await service.GetSecretReadResultAsync("bybit-1-2-api-key");

        unavailable.IsUnavailable.Should().BeTrue();
        recovered.Value.Should().Be("available-key");
        client.Verify(x => x.GetSecretAsync("bybit-1-2-api-key", null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

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
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<ILogger<KeyVaultService>>());

        service.Should().NotBeNull();
    }
}
