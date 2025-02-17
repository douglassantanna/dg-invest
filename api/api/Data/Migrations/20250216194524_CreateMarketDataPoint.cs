using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class CreateMarketDataPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketDataPoint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CoinSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CoinPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Time = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataPoint", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataPoint_AccountId",
                table: "MarketDataPoint",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataPoint_Time",
                table: "MarketDataPoint",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataPoint_UserId",
                table: "MarketDataPoint",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketDataPoint");
        }
    }
}
