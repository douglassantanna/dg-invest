using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    public partial class create_addresses_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CryptoAssetId",
                table: "CryptoTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "CryptoTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "CryptoAssets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddressName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CryptoAssetId = table.Column<int>(type: "int", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_CryptoAssets_CryptoAssetId",
                        column: x => x.CryptoAssetId,
                        principalTable: "CryptoAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_CryptoAssetId",
                table: "CryptoTransactions",
                column: "CryptoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CryptoAssetId",
                table: "Address",
                column: "CryptoAssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoTransactions_CryptoAssets_CryptoAssetId",
                table: "CryptoTransactions",
                column: "CryptoAssetId",
                principalTable: "CryptoAssets",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoTransactions_CryptoAssets_CryptoAssetId",
                table: "CryptoTransactions");

            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropIndex(
                name: "IX_CryptoTransactions_CryptoAssetId",
                table: "CryptoTransactions");

            migrationBuilder.DropColumn(
                name: "CryptoAssetId",
                table: "CryptoTransactions");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "CryptoTransactions");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "CryptoAssets");
        }
    }
}
