using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations;

// One account transaction per exchange id per account. Deposits, withdrawals, transfers and the opening balance are
// deduplicated by ExchangeTransactionId with a check-then-insert, so a manual sync or a historical import overlapping
// the timer could record the same event twice. The same id on different accounts stays valid (one account withdraws,
// the other receives), and so do empty ids (withdrawals saved before Bybit record ids were used as keys).
//
// Duplicates are financial records, so they are never deleted here: the migration stops if any exist, so they can be
// reviewed. The index is managed here in SQL and is not part of the EF model.
[DbContext(typeof(DataContext))]
[Migration("20261122090100_UniqueAccountTransactionPerAccount")]
public partial class UniqueAccountTransactionPerAccount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM dbo.AccountTransactions
                WHERE AccountId IS NOT NULL AND ExchangeTransactionId IS NOT NULL AND ExchangeTransactionId <> ''
                GROUP BY AccountId, ExchangeTransactionId
                HAVING COUNT(*) > 1)
                THROW 51004, 'AccountTransactions has the same ExchangeTransactionId more than once in one account; review and remove the duplicates before creating IX_AccountTransactions_AccountId_ExchangeTransactionId.', 1;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_AccountTransactions_AccountId_ExchangeTransactionId'
                  AND object_id = OBJECT_ID('dbo.AccountTransactions'))
                CREATE UNIQUE INDEX IX_AccountTransactions_AccountId_ExchangeTransactionId
                    ON dbo.AccountTransactions (AccountId, ExchangeTransactionId)
                    WHERE AccountId IS NOT NULL AND ExchangeTransactionId IS NOT NULL AND ExchangeTransactionId <> '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_AccountTransactions_AccountId_ExchangeTransactionId'
                  AND object_id = OBJECT_ID('dbo.AccountTransactions'))
                DROP INDEX IX_AccountTransactions_AccountId_ExchangeTransactionId ON dbo.AccountTransactions;
            """);
    }
}
