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
    private const string PreCredentialSagaMigration = "20260805134607_AddIsDeletedToAccount";
    private const string CredentialSagaMigration = "20260822192424_AddCredentialSetSaga";
    private const string CreatesAccountMigration = "20260822195335_AddCredentialOperationCreatesAccount";
    private const string PriorVersionMigration = "20260822210000_AddCredentialOperationPriorVersion";
    private const string AccountExternalIdIndexMigration = "20260822220000_AlignAccountExternalIdIndex";
    private const string LegacyPromotionMigration = "20260822230000_AddLegacyBybitCredentialPromotions";

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
    public async Task CredentialSagaMigrations_ShouldPreserveLegacyReadsAndCreateExpectedSqlServerSchema()
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

        await migrator.MigrateAsync(PreCredentialSagaMigration);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO Users (FullName, Email, Password, Role, EmailConfirmed, CreatedAt)
            VALUES ('Legacy credential user', 'legacy-credentials@example.com', 'hash', 0, 0, SYSUTCDATETIME());

            INSERT INTO Accounts (IsSelected, UserId, Balance, Name, CreatedAt, AccountType, Enabled, Exchange, IsDeleted)
            VALUES (1, 1, 0, 'Legacy Bybit', SYSUTCDATETIME(), 1, 1, 'Bybit', 0);

            INSERT INTO SyncStatuses (UserId, AccountId, ExchangeName, Status, ErrorCount)
            VALUES (1, 1, 'Bybit', 'Disconnected', 0);

            INSERT INTO ExchangeIntegrations (UserId, Exchange, Status, Enabled, CreatedDate)
            VALUES (1, 'Bybit', 'NotSetup', 1, SYSUTCDATETIME());
            """);

        await migrator.MigrateAsync(CredentialSagaMigration);
        await migrator.MigrateAsync(CreatesAccountMigration);
        await migrator.MigrateAsync(PriorVersionMigration);
        context.ChangeTracker.Clear();

        (await ScalarAsync(context, "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'CredentialUpdateOperations'")).Should().Be(1);
        (await ScalarAsync(context, """
            SELECT COUNT(*) AS Value
            FROM sys.columns AS c
            INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID(N'[CredentialUpdateOperations]')
              AND ((c.name = 'CreatesAccount' AND t.name = 'bit' AND c.is_nullable = 0)
                OR (c.name = 'PreviousCredentialVersion' AND t.name = 'uniqueidentifier' AND c.is_nullable = 1)
                OR (c.name = 'Version' AND t.name = 'uniqueidentifier' AND c.is_nullable = 0))
            """)).Should().Be(3);
        (await ScalarAsync(context, """
            SELECT COUNT(*) AS Value
            FROM sys.columns AS c
            INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
            WHERE c.object_id IN (OBJECT_ID(N'[SyncStatuses]'), OBJECT_ID(N'[ExchangeIntegrations]'))
              AND ((c.name = 'ActiveCredentialSetId' AND t.name = 'nvarchar' AND c.max_length = 64 AND c.is_nullable = 1)
                OR (c.name = 'CredentialVersion' AND t.name = 'uniqueidentifier' AND c.is_nullable = 0))
            """)).Should().Be(4);
        (await ScalarAsync(context, """
            SELECT COUNT(*) AS Value
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'[CredentialUpdateOperations]')
              AND ((name = 'IX_CredentialUpdateOperations_OperationId' AND is_unique = 1)
                OR (name = 'IX_CredentialUpdateOperations_UserId_Exchange_AccountId_State' AND is_unique = 0))
            """)).Should().Be(2);
        (await ScalarAsync(context, """
            SELECT COUNT(*) AS Value
            FROM sys.default_constraints AS d
            INNER JOIN sys.columns AS c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
            WHERE (c.object_id IN (OBJECT_ID(N'[SyncStatuses]'), OBJECT_ID(N'[ExchangeIntegrations]')) AND c.name = 'CredentialVersion')
               OR (c.object_id = OBJECT_ID(N'[CredentialUpdateOperations]') AND c.name = 'CreatesAccount')
            """)).Should().Be(3);

        var status = await context.SyncStatuses.SingleAsync();
        var integration = await context.ExchangeIntegrations.SingleAsync();
        status.ActiveCredentialSetId.Should().BeNull();
        integration.ActiveCredentialSetId.Should().BeNull();
        status.CredentialVersion.Should().Be(Guid.Empty);
        integration.CredentialVersion.Should().Be(Guid.Empty);
        context.Model.FindEntityType(typeof(SyncStatus))!.FindProperty(nameof(SyncStatus.CredentialVersion))!.IsConcurrencyToken.Should().BeTrue();
        context.Model.FindEntityType(typeof(ExchangeIntegration))!.FindProperty(nameof(ExchangeIntegration.CredentialVersion))!.IsConcurrencyToken.Should().BeTrue();

        var vault = new Mock<IKeyVaultService>();
        vault.Setup(x => x.GetSecretReadResultAsync(BybitCredentialKeys.LegacyAccountKey(1, 1, "api-key")))
            .ReturnsAsync(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "legacy-key"));

        var read = await BybitCredentialReader.ReadAsync(context, vault.Object, 1, 1, "api-key");

        read.Should().Be(new KeyVaultSecretReadResult(KeyVaultSecretReadStatus.Found, "legacy-key"));
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

        var filter = await context.Database.SqlQueryRaw<string>("""
            SELECT filter_definition AS Value
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'[Accounts]')
              AND name = 'IX_Accounts_UserId_Exchange_ExternalId'
            """).SingleAsync();
        filter.Should().Contain("[ExternalId] IS NOT NULL");
        filter.Should().Contain("[IsDeleted]=(0)");
    }

    [Fact]
    public async Task LegacyPromotionMigration_ShouldCreateDurableUniquePromotionSchema()
    {
        await using var container = new MsSqlBuilder().WithPassword($"T{Guid.NewGuid():N}aA1!").Build();
        await container.StartAsync();
        var options = new DbContextOptionsBuilder<DataContext>().UseSqlServer(container.GetConnectionString()).Options;
        await using var context = new DataContext(options);
        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(PriorVersionMigration);
        (await ScalarAsync(context, "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'LegacyBybitCredentialPromotions'")).Should().Be(0);
        await migrator.MigrateAsync(LegacyPromotionMigration);

        (await ScalarAsync(context, "SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'LegacyBybitCredentialPromotions'")).Should().Be(1);
        (await ScalarAsync(context, "SELECT COUNT(*) AS Value FROM sys.indexes WHERE object_id = OBJECT_ID(N'[LegacyBybitCredentialPromotions]') AND name = 'IX_LegacyBybitCredentialPromotions_UserId_Exchange' AND is_unique = 1")).Should().Be(1);
    }

    private static async Task<int> ScalarAsync(DataContext context, string sql) =>
        await context.Database.SqlQueryRaw<int>(sql).SingleAsync();
}
