using api.AzureKeyVault;
using api.Cache;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Data;
using api.Exchanges.Bybit;
using api.Exchanges.Models;
using api.Exchanges.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitAccountSyncServiceBackfillTests
{
    [Fact]
    public async Task BackfillAsync_PersistsTheCapturedEndAsSyncCheckpoint()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new DataContext(options);
        var account = new Account("Bybit account", 1, EAccountType.Exchange, "Bybit", "UID-001");
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var syncStatus = new SyncStatus(1, account.Id, "Bybit");
        syncStatus.EnableForCredentials();
        context.SyncStatuses.Add(syncStatus);
        await context.SaveChangesAsync();

        var keyVault = new Mock<IKeyVaultService>();
        keyVault.Setup(vault => vault.GetSecretReadResultAsync(It.IsAny<string>()))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "test-credential"));

        long? requestedEndTime = null;
        var bybitService = new Mock<IBybitService>();
        bybitService.Setup(service => service.GetOrderHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .Returns(async (string _, string _, BybitRegion _, int? _, long? _, CancellationToken token, long? endTime) =>
            {
                requestedEndTime = endTime;
                await Task.Delay(TimeSpan.FromMilliseconds(50), token);
                return [];
            });
        bybitService.Setup(service => service.GetDepositHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ReturnsAsync([]);
        bybitService.Setup(service => service.GetWithdrawalHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ReturnsAsync([]);
        bybitService.Setup(service => service.GetInternalTransferHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ReturnsAsync([]);
        bybitService.Setup(service => service.GetUniversalTransferHistoryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<int?>(),
                It.IsAny<long?>(), It.IsAny<CancellationToken>(), It.IsAny<long?>()))
            .ReturnsAsync([]);
        bybitService.Setup(service => service.GetWalletBalanceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new BybitWalletBalanceResponse());
        bybitService.Setup(service => service.GetAccountCoinBalancesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BybitRegion>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new BybitAccountCoinBalancesResponse());

        DateTime? persistedCheckpoint = null;
        var orderSyncService = new Mock<IBybitOrderSyncService>();
        orderSyncService.Setup(service => service.UpsertSyncStatusAsync(
                account.UserId,
                account.Id,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DateTime?>()))
            .Callback<int, int, string?, CancellationToken, DateTime?>((_, _, _, _, checkpoint) => persistedCheckpoint = checkpoint)
            .Returns(Task.CompletedTask);

        var service = new BybitAccountSyncService(
            bybitService.Object,
            orderSyncService.Object,
            keyVault.Object,
            context,
            Mock.Of<ICacheService>(),
            Mock.Of<ICoinMarketCapService>(),
            NullLogger<BybitAccountSyncService>.Instance);

        var result = await service.BackfillAsync(account, DateTime.UtcNow.AddMinutes(-1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        requestedEndTime.Should().NotBeNull();
        persistedCheckpoint.Should().NotBeNull();
        persistedCheckpoint!.Value.Should().BeCloseTo(
            DateTimeOffset.FromUnixTimeMilliseconds(requestedEndTime!.Value).UtcDateTime,
            TimeSpan.FromMilliseconds(2));
    }
}
