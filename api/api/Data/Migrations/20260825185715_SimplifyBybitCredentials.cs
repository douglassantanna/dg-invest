using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyBybitCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredentialUpdateOperations");

            migrationBuilder.DropTable(
                name: "LegacyBybitCredentialPromotions");

            migrationBuilder.DropColumn(
                name: "ActiveCredentialSetId",
                table: "SyncStatuses");

            migrationBuilder.DropColumn(
                name: "CredentialVersion",
                table: "SyncStatuses");

            migrationBuilder.DropColumn(
                name: "ActiveCredentialSetId",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "CredentialVersion",
                table: "ExchangeIntegrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveCredentialSetId",
                table: "SyncStatuses",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CredentialVersion",
                table: "SyncStatuses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ActiveCredentialSetId",
                table: "ExchangeIntegrations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CredentialVersion",
                table: "ExchangeIntegrations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CredentialUpdateOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatesAccount = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewCredentialSetId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OperationId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PreviousCredentialSetId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PreviousCredentialVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialUpdateOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegacyBybitCredentialPromotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CredentialOperationId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CredentialSetId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceAccountId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyBybitCredentialPromotions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CredentialUpdateOperations_OperationId",
                table: "CredentialUpdateOperations",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CredentialUpdateOperations_UserId_Exchange_AccountId_State",
                table: "CredentialUpdateOperations",
                columns: new[] { "UserId", "Exchange", "AccountId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyBybitCredentialPromotions_UserId_Exchange",
                table: "LegacyBybitCredentialPromotions",
                columns: new[] { "UserId", "Exchange" },
                unique: true);
        }
    }
}
