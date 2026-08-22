using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Commands;
using api.Exchanges.Models;
using api.Exchanges.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace unit_tests.ExchangesTests.Commands;

public class TestBybitConnectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenConnectionValidationFails_ShouldNotMarkAccountVerified()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var syncStatus = new SyncStatus(1, account.Id, "Bybit");
        context.SyncStatuses.Add(syncStatus);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "configured"));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.TestConnectionAsync("configured", "configured")).ReturnsAsync(false);
        var logger = Mock.Of<ILogger<TestBybitConnectionCommandHandler>>();
        var handler = new TestBybitConnectionCommandHandler(keyVault.Object, bybitService.Object, context, logger);

        var result = await handler.Handle(new TestBybitConnectionCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        syncStatus.LastVerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenKeyVaultIsUnavailable_ReturnsRetriableFailure()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Unavailable));
        var handler = new TestBybitConnectionCommandHandler(keyVault.Object, Mock.Of<IBybitService>(), context,
            Mock.Of<ILogger<TestBybitConnectionCommandHandler>>());

        var result = await handler.Handle(new TestBybitConnectionCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(KeyVaultSecretReadResult.UnavailableMessage);
        result.Data.Should().Be(503);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreMissing_ReturnsBadRequestWithoutCallingBybit()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.NotFound));
        var bybitService = new Mock<IBybitService>();
        var handler = new TestBybitConnectionCommandHandler(keyVault.Object, bybitService.Object, context,
            Mock.Of<ILogger<TestBybitConnectionCommandHandler>>());

        var result = await handler.Handle(new TestBybitConnectionCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().Be(400);
        bybitService.Verify(x => x.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithActiveCredentialSet_UsesImmutableApiCredentialsInsteadOfLegacyOrArbitraryKeys()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        const string credentialSetId = "active-connection-set";
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.ActivateCredentialSet(credentialSetId);
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var immutableApiKey = BybitCredentialKeys.SetKey(credentialSetId, "api-key");
        var immutableApiSecret = BybitCredentialKeys.SetKey(credentialSetId, "api-secret");
        var legacyApiKey = BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-key");
        var legacyApiSecret = BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-secret");
        var secrets = new Dictionary<string, string>
        {
            [immutableApiKey] = "immutable-api-key",
            [immutableApiSecret] = "immutable-api-secret",
            [legacyApiKey] = "conflicting-legacy-api-key",
            [legacyApiSecret] = "conflicting-legacy-api-secret"
        };
        var keyVault = new Mock<IKeyVaultService>();
        keyVault
            .Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .Returns((string key) => Task.FromResult(secrets.TryGetValue(key, out var value)
                ? new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, value)
                : throw new InvalidOperationException($"Unexpected vault key: {key}")));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.TestConnectionAsync("immutable-api-key", "immutable-api-secret")).ReturnsAsync(true);
        var handler = new TestBybitConnectionCommandHandler(keyVault.Object, bybitService.Object, context,
            Mock.Of<ILogger<TestBybitConnectionCommandHandler>>());

        var result = await handler.Handle(new TestBybitConnectionCommand(1, account.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        bybitService.Verify(x => x.TestConnectionAsync("immutable-api-key", "immutable-api-secret"), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(immutableApiKey), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(immutableApiSecret), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(legacyApiKey), Times.Never);
        keyVault.Verify(x => x.GetSecretReadResultAsync(legacyApiSecret), Times.Never);
    }
}
