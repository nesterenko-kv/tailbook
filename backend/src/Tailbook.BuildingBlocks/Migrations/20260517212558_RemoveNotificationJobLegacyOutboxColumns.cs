using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotificationJobLegacyOutboxColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventType",
                schema: "notifications",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                schema: "notifications",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "SourceOutboxMessageId",
                schema: "notifications",
                table: "notification_jobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                schema: "notifications",
                table: "notification_jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAt",
                schema: "notifications",
                table: "notification_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceOutboxMessageId",
                schema: "notifications",
                table: "notification_jobs",
                type: "uuid",
                nullable: true);
        }
    }
}
