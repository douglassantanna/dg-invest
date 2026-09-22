using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyBybitCredentialPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacyBybitCredentialPromotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceAccountId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CredentialOperationId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CredentialSetId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyBybitCredentialPromotions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyBybitCredentialPromotions_UserId_Exchange",
                table: "LegacyBybitCredentialPromotions",
                columns: new[] { "UserId", "Exchange" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyBybitCredentialPromotions");
        }
    }
}
