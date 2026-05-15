#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class FastEndpointsJobQueueStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "Jobs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueueID = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TrackingID = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecuteAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpireOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DequeueAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CommandJson = table.Column<string>(type: "text", nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_QueueID_IsComplete_ExecuteAfter_ExpireOn_DequeueAfter",
                schema: "public",
                table: "Jobs",
                columns: new[] { "QueueID", "IsComplete", "ExecuteAfter", "ExpireOn", "DequeueAfter" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_TrackingID",
                schema: "public",
                table: "Jobs",
                column: "TrackingID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jobs",
                schema: "public");
        }
    }
}
