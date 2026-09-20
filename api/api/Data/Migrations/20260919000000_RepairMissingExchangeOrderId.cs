using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using api.Data;

#nullable disable

namespace api.Migrations;

[Migration("20260919000000_RepairMissingExchangeOrderId")]
[DbContext(typeof(DataContext))]
public partial class RepairMissingExchangeOrderId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.CryptoTransactions', 'ExchangeOrderId') IS NULL
                ALTER TABLE dbo.CryptoTransactions ADD ExchangeOrderId nvarchar(100) NULL;
            """);

        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoTransactions_ExchangeOrderId'
                  AND object_id = OBJECT_ID('dbo.CryptoTransactions'))
                CREATE UNIQUE INDEX IX_CryptoTransactions_ExchangeOrderId
                    ON dbo.CryptoTransactions (ExchangeOrderId)
                    WHERE ExchangeOrderId IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration repairs schema that may predate its migration history entry.
        // Keeping the repaired objects on rollback avoids deleting existing order IDs.
    }
}
