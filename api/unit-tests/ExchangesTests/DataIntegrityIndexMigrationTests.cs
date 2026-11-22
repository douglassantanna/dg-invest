using api.Cryptos.Models;
using api.Data;
using api.Models.Cryptos;
using api.Users.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.MsSql;

namespace unit_tests.ExchangesTests;

// Migrations that add unique indexes for data the sync can otherwise duplicate when two runs overlap.
public class DataIntegrityIndexMigrationTests
{
    private const string MigrationBeforeIndexes = "20261016090000_ScopeExchangeOrderIdIndexByCryptoAsset";

    [Fact]
    public async Task CryptoAssetIndex_RemovesUnreferencedDuplicatesKeepsHistoryAndCreatesTheIndex()
    {
        await using var container = await StartSqlAsync();
        await using var context = await MigratedToBeforeIndexesAsync(container);
        var accountId = await CreateAccountAsync(context);
        // BTC: a copy with a transaction and an empty copy. ETH: two empty copies (STAGE account 34's case).
        var btcWithHistory = await InsertAssetAsync(context, accountId, "BTC", 1);
        await InsertTransactionAsync(context, btcWithHistory);
        var btcEmpty = await InsertAssetAsync(context, accountId, "BTC", 1);
        var ethFirst = await InsertAssetAsync(context, accountId, "ETH", 1027);
        var ethSecond = await InsertAssetAsync(context, accountId, "ETH", 1027);

        await context.Database.MigrateAsync();

        var remaining = await context.Database.SqlQueryRaw<int>(
            $"SELECT Id AS Value FROM dbo.CryptoAssets WHERE AccountId = {accountId} ORDER BY Id").ToListAsync();
        remaining.Should().Equal(btcWithHistory, ethFirst);
        remaining.Should().NotContain([btcEmpty, ethSecond]);
        (await IndexExistsAsync(context, "CryptoAssets", "IX_CryptoAssets_AccountId_CoinMarketCapId")).Should().BeTrue();
    }

    [Fact]
    public async Task CryptoAssetIndex_WhenBothDuplicatesHaveHistory_FailsInsteadOfDeletingData()
    {
        await using var container = await StartSqlAsync();
        await using var context = await MigratedToBeforeIndexesAsync(container);
        var accountId = await CreateAccountAsync(context);
        await InsertTransactionAsync(context, await InsertAssetAsync(context, accountId, "BTC", 1));
        await InsertTransactionAsync(context, await InsertAssetAsync(context, accountId, "BTC", 1));

        var migrate = () => context.Database.MigrateAsync();

        (await migrate.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(51003);
    }

    [Fact]
    public async Task AccountTransactionIndex_WhenAnExchangeTransactionIsDuplicatedInAnAccount_FailsInsteadOfDeletingData()
    {
        await using var container = await StartSqlAsync();
        await using var context = await MigratedToBeforeIndexesAsync(container);
        var accountId = await CreateAccountAsync(context);
        await InsertAccountTransactionAsync(context, accountId, "tx-1");
        await InsertAccountTransactionAsync(context, accountId, "tx-1");

        var migrate = () => context.Database.MigrateAsync();

        (await migrate.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(51004);
    }

    [Fact]
    public async Task AccountTransactionIndex_AllowsEmptyIdsAndTheSameIdOnDifferentAccounts()
    {
        await using var container = await StartSqlAsync();
        await using var context = await MigratedToBeforeIndexesAsync(container);
        var firstAccount = await CreateAccountAsync(context);
        var secondAccount = await CreateAccountAsync(context);
        // Withdrawals saved before per-account keys could have an empty txID; a shared txID across accounts is valid.
        await InsertAccountTransactionAsync(context, firstAccount, "");
        await InsertAccountTransactionAsync(context, firstAccount, "");
        await InsertAccountTransactionAsync(context, firstAccount, "shared-tx");
        await InsertAccountTransactionAsync(context, secondAccount, "shared-tx");

        await context.Database.MigrateAsync();

        (await IndexExistsAsync(context, "AccountTransactions", "IX_AccountTransactions_AccountId_ExchangeTransactionId")).Should().BeTrue();
        var insertDuplicate = () => InsertAccountTransactionAsync(context, firstAccount, "shared-tx");
        await insertDuplicate.Should().ThrowAsync<SqlException>();
    }

    private static async Task<MsSqlContainer> StartSqlAsync()
    {
        var container = new MsSqlBuilder().WithPassword($"T{Guid.NewGuid():N}aA1!").Build();
        await container.StartAsync();
        return container;
    }

    private static async Task<DataContext> MigratedToBeforeIndexesAsync(MsSqlContainer container)
    {
        var context = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlServer(container.GetConnectionString()).Options);
        await context.Database.GetService<IMigrator>().MigrateAsync(MigrationBeforeIndexes);
        return context;
    }

    private static async Task<int> CreateAccountAsync(DataContext context)
    {
        var user = new User("Migration User", $"migration-{Guid.NewGuid():N}@example.com", "password", Role.User);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return await context.Accounts.Where(account => account.UserId == user.Id).Select(account => account.Id).SingleAsync();
    }

    // Raw SQL: the domain (Account.AddCryptoAsset) refuses duplicates, but concurrent syncs created them in STAGE.
    private static async Task<int> InsertAssetAsync(DataContext context, int accountId, string symbol, int coinMarketCapId) =>
        (await context.Database.SqlQueryRaw<int>($"""
            INSERT INTO dbo.CryptoAssets (CryptoCurrency, Balance, TotalInvested, Symbol, CurrencyName, CreatedAt, Deleted, CoinMarketCapId, AccountId)
            OUTPUT INSERTED.Id AS Value
            VALUES ('{symbol}', 0, 0, '{symbol}', '{symbol}', SYSDATETIMEOFFSET(), 0, {coinMarketCapId}, {accountId});
            """).ToListAsync()).Single();

    private static Task InsertTransactionAsync(DataContext context, int cryptoAssetId) =>
        context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO dbo.CryptoTransactions (Amount, Price, PurchaseDate, ExchangeName, TransactionType, Enabled, Fee, CryptoAssetId)
            VALUES (1, 100, SYSDATETIMEOFFSET(), 'Test', 1, 1, 0, {cryptoAssetId});
            """);

    private static Task InsertAccountTransactionAsync(DataContext context, int accountId, string exchangeTransactionId) =>
        context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO dbo.AccountTransactions (Date, TransactionType, Amount, ExchangeName, Notes, CryptoCurrentPrice, AccountId, Fee, ExchangeTransactionId)
            VALUES (SYSDATETIME(), 1, 10, 'Bybit', 'test', 1, {accountId}, 0, '{exchangeTransactionId}');
            """);

    private static async Task<bool> IndexExistsAsync(DataContext context, string table, string index) =>
        await context.Database.SqlQueryRaw<int>(
            $"SELECT COUNT(*) AS Value FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.{table}') AND name = '{index}'").SingleAsync() == 1;
}
