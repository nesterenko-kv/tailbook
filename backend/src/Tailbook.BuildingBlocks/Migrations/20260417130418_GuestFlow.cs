using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class GuestFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PetId",
                schema: "booking",
                table: "booking_requests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "GuestIntakeJson",
                schema: "booking",
                table: "booking_requests",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredGroomerId",
                schema: "booking",
                table: "booking_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectionMode",
                schema: "booking",
                table: "booking_requests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_requests_PreferredGroomerId",
                schema: "booking",
                table: "booking_requests",
                column: "PreferredGroomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_booking_requests_PreferredGroomerId",
                schema: "booking",
                table: "booking_requests");

            migrationBuilder.DropColumn(
                name: "GuestIntakeJson",
                schema: "booking",
                table: "booking_requests");

            migrationBuilder.DropColumn(
                name: "PreferredGroomerId",
                schema: "booking",
                table: "booking_requests");

            migrationBuilder.DropColumn(
                name: "SelectionMode",
                schema: "booking",
                table: "booking_requests");

            migrationBuilder.AlterColumn<Guid>(
                name: "PetId",
                schema: "booking",
                table: "booking_requests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
