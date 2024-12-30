using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCryptoAssetListFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets");

            migrationBuilder.DropIndex(
                name: "IX_CryptoAssets_UserId",
                table: "CryptoAssets");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CryptoAssets",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CryptoAssets",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoAssets_UserId",
                table: "CryptoAssets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
