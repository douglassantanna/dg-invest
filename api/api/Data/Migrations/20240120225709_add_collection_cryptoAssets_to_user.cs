using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_collection_cryptoAssets_to_user : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "CryptoAssets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptoAssets_UserId",
                table: "CryptoAssets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets");

            migrationBuilder.DropIndex(
                name: "IX_CryptoAssets_UserId",
                table: "CryptoAssets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CryptoAssets");
        }
    }
}
