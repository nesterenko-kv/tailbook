using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class ClientAppointmentsPetStartIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_appointments_PetId_StartAt",
                schema: "booking",
                table: "appointments",
                columns: new[] { "PetId", "StartAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_appointments_PetId_StartAt",
                schema: "booking",
                table: "appointments");
        }
    }
}
