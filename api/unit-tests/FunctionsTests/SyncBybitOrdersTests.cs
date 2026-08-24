using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Models;
using api.Exchanges.Services;
using functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace unit_tests.FunctionsTests;

public class SyncBybitOrdersTests
{
    [Fact]
    public async Task Run_WithActiveCredentialSet_UsesImmutableApiCredentialsInsteadOfLegacyOrArbitraryKeys()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.ActivateCredentialSet("integration-set");
        context.AddRange(account, integration);
        await context.SaveChangesAsync();

        const string credentialSetId = "active-scheduled-sync-set";
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
        bybitService.Setup(x => x.GetOrderHistoryAsync("immutable-api-key", "immutable-api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("immutable-api-key", "immutable-api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("immutable-api-key", "immutable-api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BybitSync:Enabled"] = "true" })
            .Build();
        var function = new SyncBybitOrders(bybitService.Object, orderSyncService.Object, keyVault.Object, context,
            Mock.Of<ILogger<SyncBybitOrders>>(), configuration);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        bybitService.Verify(x => x.GetOrderHistoryAsync("immutable-api-key", "immutable-api-secret", 50, It.IsAny<long?>()), Times.Once);
        bybitService.Verify(x => x.GetDepositHistoryAsync("immutable-api-key", "immutable-api-secret", 50, It.IsAny<long?>()), Times.Once);
        bybitService.Verify(x => x.GetWithdrawalHistoryAsync("immutable-api-key", "immutable-api-secret", 50, It.IsAny<long?>()), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(immutableApiKey), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(immutableApiSecret), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(legacyApiKey), Times.Never);
        keyVault.Verify(x => x.GetSecretReadResultAsync(legacyApiSecret), Times.Never);
    }

    [Fact]
    public async Task Run_WhenIntegrationIsDisconnected_ShouldNotReadCredentialsOrCallBybit()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.ActivateCredentialSet("integration-set");
        integration.MarkDisconnected();
        var status = new SyncStatus(1, 1, "Bybit");
        status.ActivateCredentialSet("account-set");
        context.AddRange(account, integration, status);
        await context.SaveChangesAsync();
        var keyVault = new Mock<IKeyVaultService>();
        var bybitService = new Mock<IBybitService>();
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var function = new SyncBybitOrders(bybitService.Object, orderSyncService.Object, keyVault.Object, context,
            Mock.Of<ILogger<SyncBybitOrders>>(), EnabledConfiguration());
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        keyVault.Verify(x => x.GetSecretReadResultAsync(It.IsAny<string>()), Times.Never);
        bybitService.Verify(x => x.GetOrderHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<long?>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenExternalIdBelongsToManualOrOtherExchangeAccount_ShouldSkipAccount()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var manual = new Account("Manual with external", 1);
        manual.SetExternalId("manual-external");
        var otherExchange = new Account("Other exchange", 1, EAccountType.Exchange, "Binance", "binance-uid");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.ActivateCredentialSet("integration-set");
        context.AddRange(manual, otherExchange, integration);
        await context.SaveChangesAsync();
        var keyVault = new Mock<IKeyVaultService>();
        var bybitService = new Mock<IBybitService>();
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var function = new SyncBybitOrders(bybitService.Object, orderSyncService.Object, keyVault.Object, context,
            Mock.Of<ILogger<SyncBybitOrders>>(), EnabledConfiguration());
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        keyVault.Verify(x => x.GetSecretReadResultAsync(It.IsAny<string>()), Times.Never);
        bybitService.Verify(x => x.GetOrderHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<long?>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenAccountHasCredentialsButNoIntegrationRow_ShouldStillPollAccount()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.ActivateCredentialSet("account-set");
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.SetKey("account-set", "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.SetKey("account-set", "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-secret"));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync("api-key", "api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("api-key", "api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("api-key", "api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        var function = new SyncBybitOrders(bybitService.Object, Mock.Of<IBybitOrderSyncService>(), keyVault.Object, context,
            Mock.Of<ILogger<SyncBybitOrders>>(), EnabledConfiguration());
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        bybitService.Verify(x => x.GetOrderHistoryAsync("api-key", "api-secret", 50, It.IsAny<long?>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenLegacyStatusHasNoActiveSet_ShouldUseLegacyCredentials()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Legacy Futures", 1, EAccountType.Exchange, "Bybit", "UID-LEGACY");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        typeof(SyncStatus).GetProperty(nameof(SyncStatus.CredentialVersion))!.SetValue(status, Guid.Empty);
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();
        var legacyApiKey = BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-key");
        var legacyApiSecret = BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-secret");
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(legacyApiKey))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "legacy-api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(legacyApiSecret))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "legacy-api-secret"));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync("legacy-api-key", "legacy-api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("legacy-api-key", "legacy-api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("legacy-api-key", "legacy-api-secret", 50, It.IsAny<long?>())).ReturnsAsync([]);
        var function = new SyncBybitOrders(bybitService.Object, Mock.Of<IBybitOrderSyncService>(), keyVault.Object, context,
            Mock.Of<ILogger<SyncBybitOrders>>(), EnabledConfiguration());
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        bybitService.Verify(x => x.GetOrderHistoryAsync("legacy-api-key", "legacy-api-secret", 50, It.IsAny<long?>()), Times.Once);
    }

    private static IConfiguration EnabledConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["BybitSync:Enabled"] = "true" })
        .Build();
}
