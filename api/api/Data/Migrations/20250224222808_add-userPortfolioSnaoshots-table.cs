using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class adduserPortfolioSnaoshotstable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPortfolioSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Time = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPortfolioSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPortfolioSnapshots_AccountId",
                table: "UserPortfolioSnapshots",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPortfolioSnapshots_Time",
                table: "UserPortfolioSnapshots",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_UserPortfolioSnapshots_UserId",
                table: "UserPortfolioSnapshots",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPortfolioSnapshots");
        }
    }
}
