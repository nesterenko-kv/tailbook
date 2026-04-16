using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.Api.Host.Migrations;

public partial class InitialBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "integration");

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "integration",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ModuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_ModuleCode_OccurredAtUtc",
            schema: "integration",
            table: "outbox_messages",
            columns: new[] { "ModuleCode", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_ProcessedAtUtc",
            schema: "integration",
            table: "outbox_messages",
            column: "ProcessedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "integration");
    }
}
