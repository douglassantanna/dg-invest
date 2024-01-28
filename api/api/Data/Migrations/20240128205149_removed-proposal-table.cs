using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class removedproposaltable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Proposals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExcecutionTime = table.Column<int>(type: "int", nullable: false),
                    LabourValue = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    PaymentMethods = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Power = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TotalPriceProducts = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    WarrantyQtd = table.Column<int>(type: "int", nullable: false),
                    WarrantyType = table.Column<int>(type: "int", nullable: false),
                    Address_City = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Address_Number = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Address_State = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Address_Street = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Customer_Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Customer_Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Customer_Phone = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
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
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => new { x.ProposalId, x.Id });
                    table.ForeignKey(
                        name: "FK_Product_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
