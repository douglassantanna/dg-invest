using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    public partial class initalcreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CryptoTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PurchaseDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExchangeName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CryptoWallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CryptoCurrency = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Symbol = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    CurrencyName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoWallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Customer_Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Customer_Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Customer_Phone = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Address_Street = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Address_Number = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Address_City = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Address_State = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Address_ZipCode = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    WarrantyType = table.Column<int>(type: "int", nullable: false),
                    WarrantyQtd = table.Column<int>(type: "int", nullable: false),
                    ExcecutionTime = table.Column<int>(type: "int", nullable: false),
                    Power = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    TotalPriceProducts = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LabourValue = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    PaymentMethods = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    ProposalId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => new { x.ProposalId, x.Id });
                    table.ForeignKey(
                        name: "FK_Product_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id");
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CryptoTransactions");

            migrationBuilder.DropTable(
                name: "CryptoWallets");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Proposals");
        }
    }
}
