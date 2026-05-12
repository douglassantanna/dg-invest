using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeOrderIdToCryptoTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExchangeOrderId",
                table: "CryptoTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_ExchangeOrderId",
                table: "CryptoTransactions",
                column: "ExchangeOrderId",
                unique: true,
                filter: "[ExchangeOrderId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CryptoTransactions_ExchangeOrderId",
                table: "CryptoTransactions");

            migrationBuilder.DropColumn(
                name: "ExchangeOrderId",
                table: "CryptoTransactions");
        }
    }
}
