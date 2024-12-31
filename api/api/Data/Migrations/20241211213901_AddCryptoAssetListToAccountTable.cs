using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddCryptoAssetListToAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "CryptoAssets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptoAssets_AccountId",
                table: "CryptoAssets",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoAssets_Accounts_AccountId",
                table: "CryptoAssets",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoAssets_Accounts_AccountId",
                table: "CryptoAssets");

            migrationBuilder.DropIndex(
                name: "IX_CryptoAssets_AccountId",
                table: "CryptoAssets");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "CryptoAssets");
        }
    }
}
