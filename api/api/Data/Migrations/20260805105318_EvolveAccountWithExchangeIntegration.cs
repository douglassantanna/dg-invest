using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EvolveAccountWithExchangeIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_BybitUid",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "BybitUid",
                table: "Accounts",
                newName: "ExternalId");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Accounts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "SubaccountTag",
                table: "Accounts",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Accounts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Accounts
                SET AccountType = 1,
                    Exchange = 'Bybit'
                WHERE ExternalId IS NOT NULL;
                """);

            migrationBuilder.CreateTable(
                name: "ExchangeIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Exchange = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeIntegrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_Exchange_ExternalId",
                table: "Accounts",
                columns: new[] { "UserId", "Exchange", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeIntegrations_UserId_Exchange",
                table: "ExchangeIntegrations",
                columns: new[] { "UserId", "Exchange" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeIntegrations");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId_Exchange_ExternalId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Accounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Accounts",
                newName: "SubaccountTag");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "Accounts",
                newName: "BybitUid");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_BybitUid",
                table: "Accounts",
                column: "BybitUid",
                unique: true,
                filter: "[BybitUid] IS NOT NULL");
        }
    }
}
