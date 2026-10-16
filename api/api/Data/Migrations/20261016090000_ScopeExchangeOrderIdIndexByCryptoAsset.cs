using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261016090000_ScopeExchangeOrderIdIndexByCryptoAsset")]
public partial class ScopeExchangeOrderIdIndexByCryptoAsset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoTransactions_ExchangeOrderId'
                  AND object_id = OBJECT_ID('dbo.CryptoTransactions'))
                DROP INDEX IX_CryptoTransactions_ExchangeOrderId ON dbo.CryptoTransactions;

            IF EXISTS (
                SELECT 1
                FROM dbo.CryptoTransactions
                WHERE ExchangeOrderId IS NOT NULL
                GROUP BY CryptoAssetId, ExchangeOrderId
                HAVING COUNT(*) > 1)
                THROW 51001, 'CryptoTransactions contains duplicate ExchangeOrderId values for the same CryptoAssetId; resolve these duplicates before creating the unique index.', 1;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId'
                  AND object_id = OBJECT_ID('dbo.CryptoTransactions'))
                CREATE UNIQUE INDEX IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId
                    ON dbo.CryptoTransactions (CryptoAssetId, ExchangeOrderId)
                    WHERE ExchangeOrderId IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM dbo.CryptoTransactions
                WHERE ExchangeOrderId IS NOT NULL
                GROUP BY ExchangeOrderId
                HAVING COUNT(*) > 1)
                THROW 51002, 'Cannot restore global ExchangeOrderId uniqueness while duplicate IDs exist across crypto assets.', 1;

            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId'
                  AND object_id = OBJECT_ID('dbo.CryptoTransactions'))
                DROP INDEX IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId ON dbo.CryptoTransactions;

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
}
