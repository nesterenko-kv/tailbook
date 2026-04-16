using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class BookingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CancellationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "booking_requests",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreferredTimeJson = table.Column<string>(type: "jsonb", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "appointment_items",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferCodeSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OfferDisplayNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PriceSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointment_items_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "booking",
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_request_items",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RequestedNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_request_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_booking_request_items_booking_requests_BookingRequestId",
                        column: x => x.BookingRequestId,
                        principalSchema: "booking",
                        principalTable: "booking_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_items_AppointmentId",
                schema: "booking",
                table: "appointment_items",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_items_DurationSnapshotId",
                schema: "booking",
                table: "appointment_items",
                column: "DurationSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_items_PriceSnapshotId",
                schema: "booking",
                table: "appointment_items",
                column: "PriceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_BookingRequestId",
                schema: "booking",
                table: "appointments",
                column: "BookingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_GroomerId_StartAtUtc_EndAtUtc",
                schema: "booking",
                table: "appointments",
                columns: new[] { "GroomerId", "StartAtUtc", "EndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_Status_StartAtUtc",
                schema: "booking",
                table: "appointments",
                columns: new[] { "Status", "StartAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_items_BookingRequestId",
                schema: "booking",
                table: "booking_request_items",
                column: "BookingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_booking_requests_ClientId",
                schema: "booking",
                table: "booking_requests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_booking_requests_PetId",
                schema: "booking",
                table: "booking_requests",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_booking_requests_Status_CreatedAtUtc",
                schema: "booking",
                table: "booking_requests",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_items",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "booking_request_items",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "appointments",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "booking_requests",
                schema: "booking");
        }
    }
}
