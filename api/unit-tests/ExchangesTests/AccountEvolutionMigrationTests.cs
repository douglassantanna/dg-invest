using api.AzureKeyVault;
using api.Cryptos.Models;
using api.Exchanges.Models;
using api.Exchanges.Services;
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
