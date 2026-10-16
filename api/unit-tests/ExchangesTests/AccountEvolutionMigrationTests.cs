using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Exchanges.Models;
using api.Exchanges.Services;
using api.Models.Cryptos;
using api.Users.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Moq;
using Testcontainers.MsSql;

namespace unit_tests.ExchangesTests;

public class AccountEvolutionMigrationTests
{
    private const string PreEvolutionMigration = "20260803171537_AddBybitAccountManagementFields";
    private const string CurrentMigration = "20260805134607_AddIsDeletedToAccount";
    private const string AccountExternalIdIndexMigration = "20260822220000_AlignAccountExternalIdIndex";
    private const string ExchangeOnlyExternalIdIndexMigration = "20260824210000_FilterAccountExternalIdIndexToExchangeAccounts";
    private const string BeforeExchangeOrderIdRepairMigration = "20260913160700_AddExchangeExecutionId";

    [Fact]
    public async Task ExchangeOrderIdRepair_ShouldNormalizeLegacyMaxColumnAndAllowOrderIdsAcrossAccounts()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword($"T{Guid.NewGuid():N}aA1!")
            .Build();
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(container.GetConnectionString())
            .Options;
        await using var context = new DataContext(options);
        var migrator = context.Database.GetService<IMigrator>();

        await migrator.MigrateAsync(BeforeExchangeOrderIdRepairMigration);
        await context.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.CryptoTransactions', 'ExchangeOrderId') IS NULL
                ALTER TABLE dbo.CryptoTransactions ADD ExchangeOrderId nvarchar(max) NULL;
            ELSE
                ALTER TABLE dbo.CryptoTransactions ALTER COLUMN ExchangeOrderId nvarchar(max) NULL;

            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoTransactions_ExchangeOrderId'
                  AND object_id = OBJECT_ID('dbo.CryptoTransactions'))
                DROP INDEX IX_CryptoTransactions_ExchangeOrderId ON dbo.CryptoTransactions;
            """);

        var owner = new User("Order ID owner", "order-id-owner@example.com", "hash", Role.User);
        context.Users.Add(owner);
        await context.SaveChangesAsync();
        var firstAccount = new Account("Bybit account 1", owner.Id, EAccountType.Exchange, "Bybit", "uid-1");
        var secondAccount = new Account("Bybit account 2", owner.Id, EAccountType.Exchange, "Bybit", "uid-2");
        context.Accounts.AddRange(firstAccount, secondAccount);
        await context.SaveChangesAsync();

        var firstAsset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);
        var secondAsset = new CryptoAsset("Bitcoin", "Bitcoin", "BTC", 1);
        firstAccount.AddCryptoAsset(firstAsset).IsSuccess.Should().BeTrue();
        secondAccount.AddCryptoAsset(secondAsset).IsSuccess.Should().BeTrue();
        await context.SaveChangesAsync();
        const string repeatedOrderId = "2303311859593518848";
        firstAsset.AddHistoricalTransaction(new CryptoTransaction(
            0.000064m, 77142.8m, DateTimeOffset.UtcNow, "Bybit", ETransactionType.Buy, 0, repeatedOrderId));
        secondAsset.AddHistoricalTransaction(new CryptoTransaction(
            0.000064m, 77142.8m, DateTimeOffset.UtcNow, "Bybit", ETransactionType.Buy, 0, repeatedOrderId));
        await context.SaveChangesAsync();

        await context.Database.MigrateAsync();
        context.ChangeTracker.Clear();

        var columnLength = await ScalarAsync(context, """
            SELECT CONVERT(int, max_length) AS Value
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.CryptoTransactions')
              AND name = 'ExchangeOrderId'
            """);
        columnLength.Should().Be(200);

        var indexIsUnique = await ScalarAsync(context, """
            SELECT CONVERT(int, is_unique) AS Value
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.CryptoTransactions')
              AND name = 'IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId'
            """);
        indexIsUnique.Should().Be(1);
        (await context.CryptoTransactions.CountAsync(transaction => transaction.ExchangeOrderId == repeatedOrderId)).Should().Be(2);
        var indexKeyColumns = await context.Database.SqlQueryRaw<string>("""
            SELECT columns.name AS Value
            FROM sys.indexes AS indexes
            INNER JOIN sys.index_columns AS indexColumns
                ON indexColumns.object_id = indexes.object_id AND indexColumns.index_id = indexes.index_id
            INNER JOIN sys.columns AS columns
                ON columns.object_id = indexColumns.object_id AND columns.column_id = indexColumns.column_id
            WHERE indexes.object_id = OBJECT_ID(N'dbo.CryptoTransactions')
              AND indexes.name = 'IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId'
              AND indexColumns.key_ordinal > 0
            ORDER BY indexColumns.key_ordinal
            """).ToListAsync();
        indexKeyColumns.Should().Equal("CryptoAssetId", "ExchangeOrderId");
    }

    [Fact]
    public async Task ExchangeOrderIdScopeMigration_ShouldReplaceLegacyGlobalUniqueIndex()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword($"T{Guid.NewGuid():N}aA1!")
            .Build();
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(container.GetConnectionString())
            .Options;
        await using var context = new DataContext(options);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260919000000_RepairMissingExchangeOrderId");

        await context.Database.ExecuteSqlRawAsync("""
            DROP INDEX IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId ON dbo.CryptoTransactions;
            CREATE UNIQUE INDEX IX_CryptoTransactions_ExchangeOrderId
                ON dbo.CryptoTransactions (ExchangeOrderId)
                WHERE ExchangeOrderId IS NOT NULL;
            """);

        await context.Database.MigrateAsync();

        var indexes = await context.Database.SqlQueryRaw<string>("""
            SELECT name AS Value
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.CryptoTransactions')
              AND name IN ('IX_CryptoTransactions_ExchangeOrderId', 'IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId')
            """).ToListAsync();
        indexes.Should().ContainSingle().Which.Should().Be("IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId");
        var indexKeyColumns = await context.Database.SqlQueryRaw<string>("""
            SELECT columns.name AS Value
            FROM sys.indexes AS indexes
            INNER JOIN sys.index_columns AS indexColumns
                ON indexColumns.object_id = indexes.object_id AND indexColumns.index_id = indexes.index_id
            INNER JOIN sys.columns AS columns
                ON columns.object_id = indexColumns.object_id AND columns.column_id = indexColumns.column_id
            WHERE indexes.object_id = OBJECT_ID(N'dbo.CryptoTransactions')
              AND indexes.name = 'IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId'
              AND indexColumns.key_ordinal > 0
            ORDER BY indexColumns.key_ordinal
            """).ToListAsync();
        indexKeyColumns.Should().Equal("CryptoAssetId", "ExchangeOrderId");
    }

    [Fact]
    public async Task EvolveAccountMigration_ShouldPreserveBybitMappingsAndScopeExternalIds()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword($"T{Guid.NewGuid():N}aA1!")
            .Build();
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(container.GetConnectionString())
            .Options;
        await using var context = new DataContext(options);
        var migrator = context.Database.GetService<IMigrator>();

        await migrator.MigrateAsync(PreEvolutionMigration);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE Accounts ADD BybitUid nvarchar(50) NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IX_Accounts_BybitUid ON Accounts (BybitUid) WHERE BybitUid IS NOT NULL;

            INSERT INTO Users (FullName, Email, Password, Role, EmailConfirmed, CreatedAt)
            VALUES ('Legacy User', 'legacy@example.com', 'hash', 0, 0, SYSUTCDATETIME());

            INSERT INTO Accounts (IsSelected, UserId, Balance, SubaccountTag, CreatedAt, BybitUid)
            VALUES (1, 1, 0, 'Legacy Bybit', SYSUTCDATETIME(), 'UID-001');
            """);

        await migrator.MigrateAsync(CurrentMigration);
        context.ChangeTracker.Clear();

        var legacyAccount = await context.Accounts.SingleAsync();
        legacyAccount.Name.Should().Be("Legacy Bybit");
        legacyAccount.ExternalId.Should().Be("UID-001");
        legacyAccount.AccountType.Should().Be(EAccountType.Exchange);
        legacyAccount.Exchange.Should().Be("Bybit");
        legacyAccount.IsDeleted.Should().BeFalse();

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO Users (FullName, Email, Password, Role, EmailConfirmed, CreatedAt)
            VALUES ('Second User', 'second@example.com', 'hash', 0, 0, SYSUTCDATETIME());
            """);
        var secondUserId = await context.Users.Where(user => user.Email == "second@example.com").Select(user => user.Id).SingleAsync();

        context.Accounts.Add(new Account("Second User Bybit", secondUserId, EAccountType.Exchange, "Bybit", "UID-001"));
        await context.SaveChangesAsync();

        context.Accounts.Add(new Account("Duplicate Bybit", legacyAccount.UserId, EAccountType.Exchange, "Bybit", "UID-001"));
        var saveDuplicate = () => context.SaveChangesAsync();
        await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SimplifyCredentialsMigration_ShouldProduceCleanSchemaAndCanonicalReads()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword($"T{Guid.NewGuid():N}aA1!")
            .Build();
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(container.GetConnectionString())
            .Options;
        await using var context = new DataContext(options);

        await context.Database.MigrateAsync();
        context.ChangeTracker.Clear();

        (await ScalarAsync(context, "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'CredentialUpdateOperations'")).Should().Be(0);
        (await ScalarAsync(context, "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'LegacyBybitCredentialPromotions'")).Should().Be(0);
        (await ScalarAsync(context, """
            SELECT COUNT(*) AS Value
            FROM sys.columns AS c
            WHERE c.object_id IN (OBJECT_ID(N'[SyncStatuses]'), OBJECT_ID(N'[ExchangeIntegrations]'))
              AND (c.name = 'ActiveCredentialSetId' OR c.name = 'CredentialVersion')
            """)).Should().Be(0);

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO Users (FullName, Email, Password, Role, EmailConfirmed, CreatedAt)
            VALUES ('Clean credential user', 'clean@example.com', 'hash', 0, 0, SYSUTCDATETIME());

            INSERT INTO Accounts (IsSelected, UserId, Balance, Name, CreatedAt, AccountType, Enabled, Exchange, IsDeleted)
            VALUES (1, 1, 0, 'Bybit', SYSUTCDATETIME(), 1, 1, 'Bybit', 0);

            INSERT INTO SyncStatuses (UserId, AccountId, ExchangeName, Status, ErrorCount, Region)
            VALUES (1, 1, 'Bybit', 'Disconnected', 0, 'global');

            INSERT INTO ExchangeIntegrations (UserId, Exchange, Status, Enabled, CreatedDate, Region)
            VALUES (1, 'Bybit', 'NotSetup', 1, SYSUTCDATETIME(), 'global');
            """);

        var vault = new Mock<IKeyVaultService>();
        vault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, 1, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "canonical-key"));

        var read = await BybitCredentialReader.ReadAsync(vault.Object, 1, 1, "api-key");

        read.Should().Be(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "canonical-key"));
        vault.Verify(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, 1, "api-key")), Times.Once);
    }

    [Fact]
    public async Task AccountExternalIdIndexMigration_ShouldUseActiveAccountFilterOnSqlServer()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword($"T{Guid.NewGuid():N}aA1!")
            .Build();
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(container.GetConnectionString())
            .Options;
        await using var context = new DataContext(options);
        var migrator = context.Database.GetService<IMigrator>();

        await migrator.MigrateAsync(CurrentMigration);
        await migrator.MigrateAsync(AccountExternalIdIndexMigration);
        await migrator.MigrateAsync(ExchangeOnlyExternalIdIndexMigration);

        var filter = await context.Database.SqlQueryRaw<string>("""
            SELECT filter_definition AS Value
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'[Accounts]')
              AND name = 'IX_Accounts_UserId_Exchange_ExternalId'
            """).SingleAsync();
        filter.Should().Contain("[ExternalId] IS NOT NULL");
        filter.Should().Contain("[IsDeleted]=(0)");
        filter.Should().Contain("[AccountType]=(1)");

        context.Users.Add(new User("Polluted User", "polluted@example.com", "hash", Role.User));
        await context.SaveChangesAsync();
        var userId = await context.Users.Where(user => user.Email == "polluted@example.com").Select(user => user.Id).SingleAsync();
        var manual = new Account("Legacy manual", userId);
        manual.SetExchange("Bybit");
        manual.SetExternalId("UID-POLLUTED");
        var exchange = new Account("Exchange", userId, EAccountType.Exchange, "Bybit", "UID-POLLUTED");
        context.Accounts.AddRange(manual, exchange);

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task LatestMigration_ShouldDropLegacyPromotionTable()
    {
        await using var container = new MsSqlBuilder().WithPassword($"T{Guid.NewGuid():N}aA1!").Build();
        await container.StartAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlServer(container.GetConnectionString()).Options;
        await using var context = new DataContext(options);

        await context.Database.MigrateAsync();

        (await ScalarAsync(context, "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'LegacyBybitCredentialPromotions'")).Should().Be(0);
    }

    private static async Task<int> ScalarAsync(DataContext context, string sql) =>
        await context.Database.SqlQueryRaw<int>(sql).SingleAsync();
}
