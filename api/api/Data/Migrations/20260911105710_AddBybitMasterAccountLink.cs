using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBybitMasterAccountLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MasterAccountId",
                table: "ExchangeIntegrations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeIntegrations_MasterAccountId",
                table: "ExchangeIntegrations",
                column: "MasterAccountId",
                unique: true,
                filter: "[MasterAccountId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeIntegrations_Accounts_MasterAccountId",
                table: "ExchangeIntegrations",
                column: "MasterAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeIntegrations_Accounts_MasterAccountId",
                table: "ExchangeIntegrations");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeIntegrations_MasterAccountId",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "MasterAccountId",
                table: "ExchangeIntegrations");
        }
    }
}
