using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
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
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Accounts");
        }
    }
}
