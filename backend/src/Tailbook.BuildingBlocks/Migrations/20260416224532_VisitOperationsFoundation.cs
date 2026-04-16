using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class VisitOperationsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "visitops");

            migrationBuilder.CreateTable(
                name: "visits",
                schema: "visitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "visit_execution_items",
                schema: "visitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferCodeSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OfferDisplayNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PriceAmountSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceMinutesSnapshot = table.Column<int>(type: "integer", nullable: false),
                    ReservedMinutesSnapshot = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visit_execution_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visit_execution_items_visits_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "visitops",
                        principalTable: "visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visit_price_adjustments",
                schema: "visitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sign = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visit_price_adjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visit_price_adjustments_visits_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "visitops",
                        principalTable: "visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visit_performed_procedures",
                schema: "visitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitExecutionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureCodeSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcedureNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visit_performed_procedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visit_performed_procedures_visit_execution_items_VisitExecu~",
                        column: x => x.VisitExecutionItemId,
                        principalSchema: "visitops",
                        principalTable: "visit_execution_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visit_skipped_components",
                schema: "visitops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitExecutionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferVersionComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureCodeSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProcedureNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OmissionReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visit_skipped_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visit_skipped_components_visit_execution_items_VisitExecuti~",
                        column: x => x.VisitExecutionItemId,
                        principalSchema: "visitops",
                        principalTable: "visit_execution_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_visit_execution_items_AppointmentItemId",
                schema: "visitops",
                table: "visit_execution_items",
                column: "AppointmentItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visit_execution_items_VisitId",
                schema: "visitops",
                table: "visit_execution_items",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_visit_performed_procedures_VisitExecutionItemId",
                schema: "visitops",
                table: "visit_performed_procedures",
                column: "VisitExecutionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_visit_performed_procedures_VisitExecutionItemId_ProcedureId",
                schema: "visitops",
                table: "visit_performed_procedures",
                columns: new[] { "VisitExecutionItemId", "ProcedureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visit_price_adjustments_CreatedAtUtc",
                schema: "visitops",
                table: "visit_price_adjustments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_visit_price_adjustments_VisitId",
                schema: "visitops",
                table: "visit_price_adjustments",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_visit_skipped_components_VisitExecutionItemId",
                schema: "visitops",
                table: "visit_skipped_components",
                column: "VisitExecutionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_visit_skipped_components_VisitExecutionItemId_OfferVersionC~",
                schema: "visitops",
                table: "visit_skipped_components",
                columns: new[] { "VisitExecutionItemId", "OfferVersionComponentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visits_AppointmentId",
                schema: "visitops",
                table: "visits",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visits_CheckedInAtUtc",
                schema: "visitops",
                table: "visits",
                column: "CheckedInAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_visits_Status",
                schema: "visitops",
                table: "visits",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "visit_performed_procedures",
                schema: "visitops");

            migrationBuilder.DropTable(
                name: "visit_price_adjustments",
                schema: "visitops");

            migrationBuilder.DropTable(
                name: "visit_skipped_components",
                schema: "visitops");

            migrationBuilder.DropTable(
                name: "visit_execution_items",
                schema: "visitops");

            migrationBuilder.DropTable(
                name: "visits",
                schema: "visitops");
        }
    }
}
