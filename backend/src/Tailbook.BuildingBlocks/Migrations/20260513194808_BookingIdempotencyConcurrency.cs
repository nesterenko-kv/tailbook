using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class BookingIdempotencyConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_appointments_BookingRequestId",
                schema: "booking",
                table: "appointments");

            migrationBuilder.AddColumn<int>(
                name: "VersionNo",
                schema: "booking",
                table: "booking_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_BookingRequestId",
                schema: "booking",
                table: "appointments",
                column: "BookingRequestId",
                unique: true,
                filter: "\"BookingRequestId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_appointments_BookingRequestId",
                schema: "booking",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "VersionNo",
                schema: "booking",
                table: "booking_requests");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_BookingRequestId",
                schema: "booking",
                table: "appointments",
                column: "BookingRequestId");
        }
    }
}
