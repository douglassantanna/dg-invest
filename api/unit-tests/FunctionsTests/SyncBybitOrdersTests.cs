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
        context.Accounts.Add(account);
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
}
