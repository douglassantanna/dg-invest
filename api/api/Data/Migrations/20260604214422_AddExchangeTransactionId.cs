using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeTransactionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExchangeTransactionId",
                table: "AccountTransactions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransactions_ExchangeTransactionId",
                table: "AccountTransactions",
                column: "ExchangeTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountTransactions_ExchangeTransactionId",
                table: "AccountTransactions");

            migrationBuilder.DropColumn(
                name: "ExchangeTransactionId",
                table: "AccountTransactions");
        }
    }
}
