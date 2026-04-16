using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class StaffSchedulingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "staff");

            migrationBuilder.CreateTable(
                name: "staff_groomers",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_groomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_groomer_capabilities",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    BreedId = table.Column<Guid>(type: "uuid", nullable: true),
                    BreedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoatTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SizeCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapabilityMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReservedDurationModifierMinutes = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_groomer_capabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_groomer_capabilities_staff_groomers_GroomerId",
                        column: x => x.GroomerId,
                        principalSchema: "staff",
                        principalTable: "staff_groomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_time_blocks",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_time_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_time_blocks_staff_groomers_GroomerId",
                        column: x => x.GroomerId,
                        principalSchema: "staff",
                        principalTable: "staff_groomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staff_working_schedules",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weekday = table.Column<int>(type: "integer", nullable: false),
                    StartLocalTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndLocalTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_working_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_working_schedules_staff_groomers_GroomerId",
                        column: x => x.GroomerId,
                        principalSchema: "staff",
                        principalTable: "staff_groomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staff_groomer_capabilities_GroomerId",
                schema: "staff",
                table: "staff_groomer_capabilities",
                column: "GroomerId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_groomer_capabilities_GroomerId_OfferId_CapabilityMode",
                schema: "staff",
                table: "staff_groomer_capabilities",
                columns: new[] { "GroomerId", "OfferId", "CapabilityMode" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_groomers_Active_DisplayName",
                schema: "staff",
                table: "staff_groomers",
                columns: new[] { "Active", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_groomers_UserId",
                schema: "staff",
                table: "staff_groomers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_time_blocks_GroomerId_StartAtUtc_EndAtUtc",
                schema: "staff",
                table: "staff_time_blocks",
                columns: new[] { "GroomerId", "StartAtUtc", "EndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_working_schedules_GroomerId_Weekday",
                schema: "staff",
                table: "staff_working_schedules",
                columns: new[] { "GroomerId", "Weekday" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_groomer_capabilities",
                schema: "staff");

            migrationBuilder.DropTable(
                name: "staff_time_blocks",
                schema: "staff");

            migrationBuilder.DropTable(
                name: "staff_working_schedules",
                schema: "staff");

            migrationBuilder.DropTable(
                name: "staff_groomers",
                schema: "staff");
        }
    }
}
