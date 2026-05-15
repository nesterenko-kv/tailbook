#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class NotificationRetryPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_jobs_NextAttemptAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                column: "NextAttemptAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_jobs_NextAttemptAtUtc",
                schema: "notifications",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAtUtc",
                schema: "notifications",
                table: "notification_jobs");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                schema: "notifications",
                table: "notification_jobs");
        }
    }
}
