using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class IdentityMfaChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iam_mfa_challenges",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactorType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InvalidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastFailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequestIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_mfa_challenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_challenges_FactorId",
                schema: "iam",
                table: "iam_mfa_challenges",
                column: "FactorId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_challenges_UserId",
                schema: "iam",
                table: "iam_mfa_challenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_challenges_UserId_FactorType_ExpiresAt",
                schema: "iam",
                table: "iam_mfa_challenges",
                columns: new[] { "UserId", "FactorType", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iam_mfa_challenges",
                schema: "iam");
        }
    }
}
