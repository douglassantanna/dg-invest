using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20260824210000_FilterAccountExternalIdIndexToExchangeAccounts")]
    public partial class FilterAccountExternalIdIndexToExchangeAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId_Exchange_ExternalId",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_Exchange_ExternalId",
                table: "Accounts",
                columns: new[] { "UserId", "Exchange", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL AND [IsDeleted] = 0 AND [AccountType] = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId_Exchange_ExternalId",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_Exchange_ExternalId",
                table: "Accounts",
                columns: new[] { "UserId", "Exchange", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL AND [IsDeleted] = 0");
        }
    }
}
