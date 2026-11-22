using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations;

// One crypto asset per coin per account. Two syncs of the same account running at the same time could each create
// the coin (Account.AddCryptoAsset only checks in memory), so STAGE account 34 had every coin twice. Each copy got the
// full wallet balance, which double-counted the account's value.
//
// Before creating the index, duplicates that nothing references (no crypto transactions, account transactions or
// addresses) are deleted, keeping the copy with history, or the lowest id. If two copies both hold data, the migration
// stops so they can be merged by hand. Like IX_CryptoTransactions_CryptoAssetId_ExchangeOrderId, the index is managed
// here in SQL and is not part of the EF model.
[DbContext(typeof(DataContext))]
[Migration("20261122090000_UniqueCryptoAssetPerAccountCoin")]
public partial class UniqueCryptoAssetPerAccountCoin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH Ranked AS (
                SELECT asset.Id,
                       ROW_NUMBER() OVER (
                           PARTITION BY asset.AccountId, asset.CoinMarketCapId
                           ORDER BY CASE WHEN EXISTS (SELECT 1 FROM dbo.CryptoTransactions tx WHERE tx.CryptoAssetId = asset.Id) THEN 0 ELSE 1 END,
                                    asset.Id) AS Position
                FROM dbo.CryptoAssets asset
                WHERE asset.Deleted = 0 AND asset.AccountId IS NOT NULL AND asset.CoinMarketCapId <> 0)
            DELETE asset
            FROM dbo.CryptoAssets asset
            INNER JOIN Ranked ON Ranked.Id = asset.Id
            WHERE Ranked.Position > 1
              AND NOT EXISTS (SELECT 1 FROM dbo.CryptoTransactions tx WHERE tx.CryptoAssetId = asset.Id)
              AND NOT EXISTS (SELECT 1 FROM dbo.AccountTransactions accountTx WHERE accountTx.CryptoAssetId = asset.Id)
              AND NOT EXISTS (SELECT 1 FROM dbo.Address address WHERE address.CryptoAssetId = asset.Id);

            IF EXISTS (
                SELECT 1
                FROM dbo.CryptoAssets
                WHERE Deleted = 0 AND AccountId IS NOT NULL AND CoinMarketCapId <> 0
                GROUP BY AccountId, CoinMarketCapId
                HAVING COUNT(*) > 1)
                THROW 51003, 'CryptoAssets has the same coin more than once in one account, and more than one copy holds transactions or addresses; merge them before creating IX_CryptoAssets_AccountId_CoinMarketCapId.', 1;

            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoAssets_AccountId_CoinMarketCapId'
                  AND object_id = OBJECT_ID('dbo.CryptoAssets'))
                CREATE UNIQUE INDEX IX_CryptoAssets_AccountId_CoinMarketCapId
                    ON dbo.CryptoAssets (AccountId, CoinMarketCapId)
                    WHERE Deleted = 0 AND AccountId IS NOT NULL AND CoinMarketCapId <> 0;
            """);
    }

    // Deleted duplicates are not restored.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_CryptoAssets_AccountId_CoinMarketCapId'
                  AND object_id = OBJECT_ID('dbo.CryptoAssets'))
                DROP INDEX IX_CryptoAssets_AccountId_CoinMarketCapId ON dbo.CryptoAssets;
            """);
    }
}
