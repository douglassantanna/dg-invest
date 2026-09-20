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
using Microsoft.Extensions.Logging.Abstractions;

namespace unit_tests.FunctionsTests;

public class SyncBybitOrdersTests
{
    [Fact]
    public async Task Run_WithAccountCredentials_UsesCanonicalAccountKeysOverIntegrationKeys()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        context.AddRange(account, integration);
        await context.SaveChangesAsync();

        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var accountApiKey = BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-key");
        var accountApiSecret = BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-secret");
        var integrationApiKey = BybitCredentialKeys.LegacyIntegrationKey(1, "api-key");
        var integrationApiSecret = BybitCredentialKeys.LegacyIntegrationKey(1, "api-secret");
        var secrets = new Dictionary<string, string>
        {
            [accountApiKey] = "account-api-key",
            [accountApiSecret] = "account-api-secret",
            [integrationApiKey] = "integration-api-key",
            [integrationApiSecret] = "integration-api-secret"
        };
        var keyVault = new Mock<IKeyVaultService>();
        keyVault
            .Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .Returns((string key) => Task.FromResult(secrets.TryGetValue(key, out var value)
                ? new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, value)
                : throw new InvalidOperationException($"Unexpected vault key: {key}")));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync("account-api-key", "account-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("account-api-key", "account-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("account-api-key", "account-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetUniversalTransferHistoryAsync("integration-api-key", "integration-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        bybitService.Verify(x => x.GetOrderHistoryAsync("account-api-key", "account-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
        bybitService.Verify(x => x.GetDepositHistoryAsync("account-api-key", "account-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
        bybitService.Verify(x => x.GetWithdrawalHistoryAsync("account-api-key", "account-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(accountApiKey), Times.Exactly(2));
        keyVault.Verify(x => x.GetSecretReadResultAsync(accountApiSecret), Times.Exactly(2));
        keyVault.Verify(x => x.GetSecretReadResultAsync(integrationApiKey), Times.Once);
        keyVault.Verify(x => x.GetSecretReadResultAsync(integrationApiSecret), Times.Once);
        bybitService.Verify(x => x.GetUniversalTransferHistoryAsync("integration-api-key", "integration-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
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
        integration.MarkEnabled();
        integration.MarkDisconnected();
        var status = new SyncStatus(1, 1, "Bybit");
        status.EnableForCredentials();
        context.AddRange(account, integration, status);
        await context.SaveChangesAsync();
        var keyVault = new Mock<IKeyVaultService>();
        var bybitService = new Mock<IBybitService>();
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        keyVault.Verify(x => x.GetSecretReadResultAsync(It.IsAny<string>()), Times.Never);
        bybitService.Verify(x => x.GetOrderHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(), It.IsAny<long?>()), Times.Never);
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
        integration.MarkEnabled();
        context.AddRange(manual, otherExchange, integration);
        await context.SaveChangesAsync();
        var keyVault = new Mock<IKeyVaultService>();
        var bybitService = new Mock<IBybitService>();
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        keyVault.Verify(x => x.GetSecretReadResultAsync(It.IsAny<string>()), Times.Never);
        bybitService.Verify(x => x.GetOrderHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(), It.IsAny<long?>()), Times.Never);
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
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();
        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-secret"));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        var function = CreateFunction(bybitService.Object, Mock.Of<IBybitOrderSyncService>(), keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        bybitService.Verify(x => x.GetOrderHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenAccountHasStatus_ShouldUseCanonicalCredentials()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Legacy Futures", 1, EAccountType.Exchange, "Bybit", "UID-LEGACY");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
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
        bybitService.Setup(x => x.GetOrderHistoryAsync("legacy-api-key", "legacy-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("legacy-api-key", "legacy-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("legacy-api-key", "legacy-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        var function = CreateFunction(bybitService.Object, Mock.Of<IBybitOrderSyncService>(), keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        bybitService.Verify(x => x.GetOrderHistoryAsync("legacy-api-key", "legacy-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenAccountBalanceIsZero_ShouldPopulateStablecoinCashBeforeProcessingOrders()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        context.AddRange(account, integration);
        await context.SaveChangesAsync();

        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-secret"));

        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetAccountCoinBalanceAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), "FUND", "USDT", "UID-001"))
            .ReturnsAsync(AccountCoinBalance("FUND", "USDT", "1000", "UID-001"));
        bybitService.Setup(x => x.GetAccountCoinBalanceAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), "FUND", "USDC", "UID-001"))
            .ReturnsAsync(AccountCoinBalance("FUND", "USDC", "0", "UID-001"));
        bybitService.Setup(x => x.GetAccountCoinBalanceAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), "UNIFIED", "USDT", "UID-001"))
            .ReturnsAsync(AccountCoinBalance("UNIFIED", "USDT", "6000", "UID-001"));
        bybitService.Setup(x => x.GetAccountCoinBalanceAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), "UNIFIED", "USDC", "UID-001"))
            .ReturnsAsync(AccountCoinBalance("UNIFIED", "USDC", "4000", "UID-001"));
        bybitService.Setup(x => x.GetOrderHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        orderSyncService.Setup(x => x.ProcessOpeningBalanceAsync(account, 1, 11000m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        orderSyncService.Verify(x => x.ProcessOpeningBalanceAsync(account, 1, 11000m, It.IsAny<CancellationToken>()), Times.Once);
        bybitService.Verify(x => x.GetOrderHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenInternalTransfersExist_ShouldProcessTransfersForAccount()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        context.AddRange(account, integration);
        await context.SaveChangesAsync();

        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-secret"));

        var transfer = new BybitInternalTransferRow
        {
            TransferId = "transfer-1",
            Coin = "USDT",
            Amount = "250",
            ToAccountType = "UNIFIED",
            ToMemberId = "UID-001",
            Timestamp = "1790000000000"
        };
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetInternalTransferHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([transfer]);
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        orderSyncService.Setup(x => x.ProcessInternalTransferAsync(transfer, account, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        orderSyncService.Verify(x => x.ProcessInternalTransferAsync(transfer, account, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenUniversalTransfersExist_ShouldUseIntegrationCredentials()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var sourceAccount = new Account("Master", 1, EAccountType.Exchange, "Bybit", "UID-MASTER");
        var destinationAccount = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        context.AddRange(sourceAccount, destinationAccount, integration);
        await context.SaveChangesAsync();

        var sourceStatus = new SyncStatus(1, sourceAccount.Id, "Bybit");
        sourceStatus.EnableForCredentials();
        var destinationStatus = new SyncStatus(1, destinationAccount.Id, "Bybit");
        destinationStatus.EnableForCredentials();
        context.SyncStatuses.AddRange(sourceStatus, destinationStatus);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, sourceAccount.Id, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "source-api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, sourceAccount.Id, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "source-api-secret"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, destinationAccount.Id, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "destination-api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, destinationAccount.Id, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "destination-api-secret"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(1, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "integration-api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyIntegrationKey(1, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "integration-api-secret"));

        var transfer = new BybitInternalTransferRow
        {
            TransferId = "universal-transfer-1",
            Coin = "USDC",
            Amount = "125",
            FromAccountType = "FUND",
            FromMemberId = "UID-MASTER",
            ToAccountType = "UNIFIED",
            ToMemberId = "UID-001",
            Timestamp = "1790000000000"
        };
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetInternalTransferHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetUniversalTransferHistoryAsync("integration-api-key", "integration-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([transfer]);
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        orderSyncService.Setup(x => x.ProcessInternalTransferAsync(transfer, sourceAccount, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        orderSyncService.Setup(x => x.ProcessInternalTransferAsync(transfer, destinationAccount, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        bybitService.Verify(x => x.GetUniversalTransferHistoryAsync("integration-api-key", "integration-api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()), Times.Once);
        orderSyncService.Verify(x => x.ProcessInternalTransferAsync(transfer, sourceAccount, 1, It.IsAny<CancellationToken>()), Times.Once);
        orderSyncService.Verify(x => x.ProcessInternalTransferAsync(transfer, destinationAccount, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenUniversalTransferSourceIsNotLinked_ShouldSkipTransfer()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Subaccount", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        context.AddRange(account, integration);
        await context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "credential"));
        var transfer = new BybitInternalTransferRow
        {
            TransferId = "unlinked-transfer",
            Coin = "USDC",
            Amount = "10",
            FromAccountType = "FUND",
            FromMemberId = "UID-MISSING",
            ToAccountType = "FUND",
            ToMemberId = "UID-001",
            Timestamp = "1790000000000"
        };
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetInternalTransferHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetUniversalTransferHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([transfer]);
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        orderSyncService.Verify(x => x.ProcessInternalTransferAsync(It.IsAny<BybitInternalTransferRow>(), It.IsAny<Account>(), 1, It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(integration.LastSyncAt);
    }

    [Fact]
    public async Task Run_WhenBybitRejectsHistoryRequest_ShouldRecordBybitError()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        context.AddRange(account, integration);
        await context.SaveChangesAsync();

        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-key"));
        keyVault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, account.Id, "api-secret")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "api-secret"));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync("api-key", "api-secret", It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()))
            .ThrowsAsync(new BybitApiException(10003, "API key is invalid"));
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        var function = CreateFunction(bybitService.Object, orderSyncService.Object, keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        orderSyncService.Verify(x => x.MarkSyncStatusErrorAsync(1, account.Id,
            "Bybit rejected sync request: 10003 - API key is invalid", It.IsAny<CancellationToken>()), Times.Once);
        bybitService.Verify(x => x.GetDepositHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(), It.IsAny<long?>()), Times.Never);
        bybitService.Verify(x => x.GetWithdrawalHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(), It.IsAny<long?>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenUniversalTransferCredentialsAreRejected_ShouldMarkIntegrationErrorAndContinue()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new DataContext(options);
        var account = new Account("Futures", 1, EAccountType.Exchange, "Bybit", "UID-001");
        var integration = new ExchangeIntegration(1, "Bybit");
        integration.MarkEnabled();
        context.AddRange(account, integration);
        await context.SaveChangesAsync();
        var status = new SyncStatus(1, account.Id, "Bybit");
        status.EnableForCredentials();
        context.SyncStatuses.Add(status);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(x => x.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "credential"));
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(x => x.GetOrderHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetDepositHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetWithdrawalHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetInternalTransferHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>())).ReturnsAsync([]);
        bybitService.Setup(x => x.GetUniversalTransferHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), 50, It.IsAny<long?>()))
            .ThrowsAsync(new BybitApiException(10003, "API key is invalid"));
        var function = CreateFunction(bybitService.Object, Mock.Of<IBybitOrderSyncService>(), keyVault.Object, context);
        var functionContext = new Mock<FunctionContext>();
        functionContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await function.Run(null!, functionContext.Object);

        integration.Status.Should().Be("Error");
        integration.Enabled.Should().BeTrue();
    }

    private static IConfiguration EnabledConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["BybitSync:Enabled"] = "true" })
        .Build();

    private static SyncBybitOrders CreateFunction(
        IBybitService bybitService,
        IBybitOrderSyncService orderSyncService,
        IKeyVaultService keyVaultService,
        DataContext context) => new(
            bybitService,
            orderSyncService,
            new BybitAccountSyncService(
                bybitService,
                orderSyncService,
                keyVaultService,
                context,
                NullLogger<BybitAccountSyncService>.Instance),
            keyVaultService,
            context,
            Mock.Of<ILogger<SyncBybitOrders>>(),
            EnabledConfiguration());

    private static BybitAccountCoinBalanceResponse AccountCoinBalance(string accountType, string coin, string balance, string memberId = "") => new()
    {
        RetCode = 0,
        RetMsg = "success",
        Result = new BybitAccountCoinBalanceResult
        {
            AccountType = accountType,
            MemberId = memberId,
            Balance = new BybitAccountCoinBalance
            {
                Coin = coin,
                WalletBalance = balance,
                TransferBalance = balance
            }
        }
    };
}
