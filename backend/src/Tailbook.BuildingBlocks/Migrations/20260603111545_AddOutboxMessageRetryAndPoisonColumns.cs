using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageRetryAndPoisonColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPoisoned",
                schema: "integration",
                table: "outbox_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                schema: "integration",
                table: "outbox_messages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRetryAt",
                schema: "integration",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PoisonedAt",
                schema: "integration",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "integration",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Received"),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsPoisoned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PoisonedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt_IsPoisoned_NextRetryAt",
                schema: "integration",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "IsPoisoned", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_ConsumerName_Status_ReceivedAt",
                schema: "integration",
                table: "inbox_messages",
                columns: new[] { "ConsumerName", "Status", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_MessageId_ConsumerName",
                schema: "integration",
                table: "inbox_messages",
                columns: new[] { "MessageId", "ConsumerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_Status_NextRetryAt",
                schema: "integration",
                table: "inbox_messages",
                columns: new[] { "Status", "NextRetryAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "integration");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedAt_IsPoisoned_NextRetryAt",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "IsPoisoned",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LastError",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "PoisonedAt",
                schema: "integration",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "integration",
                table: "outbox_messages");
        }
    }
}
