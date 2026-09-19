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
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoTransactions_ExchangeOrderId'
                  AND object_id = OBJECT_ID('dbo.CryptoTransactions'))
                DROP INDEX IX_CryptoTransactions_ExchangeOrderId ON dbo.CryptoTransactions;

            IF COL_LENGTH('dbo.CryptoTransactions', 'ExchangeOrderId') IS NOT NULL
                ALTER TABLE dbo.CryptoTransactions DROP COLUMN ExchangeOrderId;
            """);
    }
}
