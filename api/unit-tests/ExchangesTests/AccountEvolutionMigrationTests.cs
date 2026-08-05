using api.Cryptos.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.MsSql;

namespace unit_tests.ExchangesTests;

public class AccountEvolutionMigrationTests
{
    private const string PreEvolutionMigration = "20260803171537_AddBybitAccountManagementFields";
    private const string CurrentMigration = "20260805134607_AddIsDeletedToAccount";

    [Fact]
    public async Task EvolveAccountMigration_ShouldPreserveBybitMappingsAndScopeExternalIds()
    {
        await using var container = new MsSqlBuilder()
            .WithPassword("P@ssw0rd!123")
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
            CREATE UNIQUE INDEX IX_Accounts_BybitUid ON Accounts (BybitUid) WHERE BybitUid IS NOT NULL;

            INSERT INTO Users (FullName, Email, Password, Role, EmailConfirmed, CreatedAt)
            VALUES ('Legacy User', 'legacy@example.com', 'hash', 0, 0, SYSUTCDATETIME());

            INSERT INTO Accounts (IsSelected, UserId, Balance, SubaccountTag, CreatedAt, BybitUid)
            VALUES (1, 1, 0, 'Legacy Bybit', SYSUTCDATETIME(), 'UID-001');
            """);

        await migrator.MigrateAsync(CurrentMigration);
        context.ChangeTracker.Clear();

        var legacyAccount = await context.Accounts.SingleAsync();
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
}
