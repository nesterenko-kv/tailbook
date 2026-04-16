using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "iam");

            migrationBuilder.EnsureSchema(
                name: "integration");

            migrationBuilder.CreateTable(
                name: "access_audit_entries",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    HappenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_audit_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_permissions",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_role_assignments",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScopeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_role_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_role_permissions",
                schema: "iam",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_role_permissions", x => new { x.RoleId, x.PermissionId });
                });

            migrationBuilder.CreateTable(
                name: "iam_roles",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "iam_users",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_users", x => x.Id);
                });

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
                name: "IX_access_audit_entries_HappenedAtUtc",
                schema: "audit",
                table: "access_audit_entries",
                column: "HappenedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_entries_ResourceType_ResourceId_HappenedAtUtc",
                schema: "audit",
                table: "access_audit_entries",
                columns: new[] { "ResourceType", "ResourceId", "HappenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_permissions_Code",
                schema: "iam",
                table: "iam_permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_role_assignments_UserId_RoleId_ScopeType_ScopeId",
                schema: "iam",
                table: "iam_role_assignments",
                columns: new[] { "UserId", "RoleId", "ScopeType", "ScopeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_roles_Code",
                schema: "iam",
                table: "iam_roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_users_NormalizedEmail",
                schema: "iam",
                table: "iam_users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_users_SubjectId",
                schema: "iam",
                table: "iam_users",
                column: "SubjectId",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_audit_entries",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "iam_permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "iam_role_assignments",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "iam_role_permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "iam_roles",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "iam_users",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "integration");
        }
    }
}
