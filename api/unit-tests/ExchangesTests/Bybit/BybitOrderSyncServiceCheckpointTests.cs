using api.AzureStorage;
using api.AzureStorage.Blob;
using api.Cache;
using api.CoinMarketCap.Service;
using api.Cryptos.Models;
using api.Cryptos.TransactionStrategies.Contracts;
using api.Data;
using api.Exchanges.Models;
using api.Exchanges.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace unit_tests.ExchangesTests.Bybit;

public class BybitOrderSyncServiceCheckpointTests
{
    [Fact]
    public async Task UpsertSyncStatusAsync_WithOlderBackfillCheckpoint_PreservesConcurrentCheckpoint()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var initialCheckpoint = DateTime.UtcNow.AddMinutes(-10);
        var concurrentCheckpoint = initialCheckpoint.AddMinutes(5);
        var backfillCheckpoint = initialCheckpoint.AddMinutes(2);

        int accountId;
        await using (var seedContext = new DataContext(options))
        {
            var account = new Account("Bybit account", 1, EAccountType.Exchange, "Bybit", "UID-001");
            seedContext.Accounts.Add(account);
            await seedContext.SaveChangesAsync();
            accountId = account.Id;

            var status = new SyncStatus(account.UserId, account.Id, "Bybit");
            status.MarkConnected("initial-order", initialCheckpoint);
            seedContext.SyncStatuses.Add(status);
            await seedContext.SaveChangesAsync();
        }

        await using var backfillContext = new DataContext(options);
        var trackedStatus = await backfillContext.SyncStatuses
            .SingleAsync(status => status.AccountId == accountId && status.ExchangeName == "Bybit");
        trackedStatus.LastSyncAt.Should().Be(initialCheckpoint);

        await using (var concurrentContext = new DataContext(options))
        {
            var concurrentStatus = await concurrentContext.SyncStatuses
                .SingleAsync(status => status.AccountId == accountId && status.ExchangeName == "Bybit");
            concurrentStatus.MarkConnected("concurrent-order", concurrentCheckpoint);
            await concurrentContext.SaveChangesAsync();
        }

        var service = new BybitOrderSyncService(
            Mock.Of<ICoinMarketCapService>(),
            Mock.Of<ITransactionService>(),
            Mock.Of<IBlobStorageService>(),
            Options.Create(new AzureStorageSettings()),
            backfillContext,
            NullLogger<BybitOrderSyncService>.Instance,
            Mock.Of<ICacheService>());

        await service.UpsertSyncStatusAsync(
            1,
            accountId,
            "backfill-order",
            CancellationToken.None,
            backfillCheckpoint);

        await using var verificationContext = new DataContext(options);
        var persistedStatus = await verificationContext.SyncStatuses
            .SingleAsync(status => status.AccountId == accountId && status.ExchangeName == "Bybit");
        persistedStatus.LastSyncAt.Should().Be(concurrentCheckpoint);
    }
}
