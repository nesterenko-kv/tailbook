using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class IdentityMfaRecoveryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iam_mfa_recovery_codes",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CodeSuffix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsumedChallengeId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvalidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_mfa_recovery_codes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_recovery_codes_BatchId",
                schema: "iam",
                table: "iam_mfa_recovery_codes",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_recovery_codes_UserId",
                schema: "iam",
                table: "iam_mfa_recovery_codes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_recovery_codes_UserId_ConsumedAt_InvalidatedAt",
                schema: "iam",
                table: "iam_mfa_recovery_codes",
                columns: new[] { "UserId", "ConsumedAt", "InvalidatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iam_mfa_recovery_codes",
                schema: "iam");
        }
    }
}
