using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBybitIntegrationAutoPause : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoPausedAt",
                table: "ExchangeIntegrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveTransportFailures",
                table: "ExchangeIntegrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastErrorAccountId",
                table: "ExchangeIntegrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastErrorAt",
                table: "ExchangeIntegrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorCode",
                table: "ExchangeIntegrations",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorEndpoint",
                table: "ExchangeIntegrations",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorMessage",
                table: "ExchangeIntegrations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPauseNotificationAttemptAt",
                table: "ExchangeIntegrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PauseNotificationAttempts",
                table: "ExchangeIntegrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PauseNotificationSentAt",
                table: "ExchangeIntegrations",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPausedAt",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "ConsecutiveTransportFailures",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "LastErrorAccountId",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "LastErrorAt",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "LastErrorCode",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "LastErrorEndpoint",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "LastErrorMessage",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "LastPauseNotificationAttemptAt",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "PauseNotificationAttempts",
                table: "ExchangeIntegrations");

            migrationBuilder.DropColumn(
                name: "PauseNotificationSentAt",
                table: "ExchangeIntegrations");
        }
    }
}
