using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_collection_cryptoAssets_to_user_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CryptoAssets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CryptoAssets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoAssets_Users_UserId",
                table: "CryptoAssets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
