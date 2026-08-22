using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialSetSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                    OperationId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PreviousCredentialSetId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewCredentialSetId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialUpdateOperations", x => x.Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredentialUpdateOperations");

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
    }
}
