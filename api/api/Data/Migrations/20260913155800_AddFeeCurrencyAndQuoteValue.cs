using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeCurrencyAndQuoteValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeeCurrency",
                table: "CryptoTransactions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeQuoteValue",
                table: "CryptoTransactions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeeCurrency",
                table: "AccountTransactions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeQuoteValue",
                table: "AccountTransactions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeCurrency",
                table: "CryptoTransactions");

            migrationBuilder.DropColumn(
                name: "FeeQuoteValue",
                table: "CryptoTransactions");

            migrationBuilder.DropColumn(
                name: "FeeCurrency",
                table: "AccountTransactions");

            migrationBuilder.DropColumn(
                name: "FeeQuoteValue",
                table: "AccountTransactions");
        }
    }
}
