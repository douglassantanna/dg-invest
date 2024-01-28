using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class sqlitemigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cryptos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    Symbol = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    Image = table.Column<string>(type: "varchar", maxLength: 1000, nullable: false),
                    CoinMarketCapId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cryptos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CryptoAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CryptoCurrency = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    AveragePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Symbol = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    CurrencyName = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoinMarketCapId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalInvested = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CryptoAssets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AddressName = table.Column<string>(type: "TEXT", nullable: false),
                    AddressValue = table.Column<string>(type: "TEXT", nullable: false),
                    CryptoAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_CryptoAssets_CryptoAssetId",
                        column: x => x.CryptoAssetId,
                        principalTable: "CryptoAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CryptoTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    PurchaseDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExchangeName = table.Column<string>(type: "varchar", maxLength: 255, nullable: false),
                    TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CryptoAssetId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CryptoTransactions_CryptoAssets_CryptoAssetId",
                        column: x => x.CryptoAssetId,
                        principalTable: "CryptoAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Address_CryptoAssetId",
                table: "Address",
                column: "CryptoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoAssets_UserId",
                table: "CryptoAssets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_CryptoAssetId",
                table: "CryptoTransactions",
                column: "CryptoAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "Cryptos");

            migrationBuilder.DropTable(
                name: "CryptoTransactions");

            migrationBuilder.DropTable(
                name: "CryptoAssets");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
