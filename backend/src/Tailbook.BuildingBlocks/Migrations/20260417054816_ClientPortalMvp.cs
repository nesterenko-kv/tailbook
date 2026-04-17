using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class ClientPortalMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                schema: "iam",
                table: "iam_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContactPersonId",
                schema: "iam",
                table: "iam_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_iam_users_ClientId",
                schema: "iam",
                table: "iam_users",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_users_ContactPersonId",
                schema: "iam",
                table: "iam_users",
                column: "ContactPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_iam_users_ClientId",
                schema: "iam",
                table: "iam_users");

            migrationBuilder.DropIndex(
                name: "IX_iam_users_ContactPersonId",
                schema: "iam",
                table: "iam_users");

            migrationBuilder.DropColumn(
                name: "ClientId",
                schema: "iam",
                table: "iam_users");

            migrationBuilder.DropColumn(
                name: "ContactPersonId",
                schema: "iam",
                table: "iam_users");
        }
    }
}
