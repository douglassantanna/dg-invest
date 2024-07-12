using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class addcryptoassettoaccounttransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CryptoAssetId",
                table: "AccountTransactions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_CryptoAssetId",
                table: "AccountTransactions",
                column: "CryptoAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountTransactions_CryptoAssets_CryptoAssetId",
                table: "AccountTransactions",
                column: "CryptoAssetId",
                principalTable: "CryptoAssets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountTransactions_CryptoAssets_CryptoAssetId",
                table: "AccountTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_CryptoAssetId",
                table: "AccountTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "CryptoAssetId",
                table: "AccountTransactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
