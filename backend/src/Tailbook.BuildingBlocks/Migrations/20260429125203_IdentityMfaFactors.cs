using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class IdentityMfaFactors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iam_mfa_factors",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactorType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnabledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisabledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_mfa_factors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_factors_Status_FactorType",
                schema: "iam",
                table: "iam_mfa_factors",
                columns: new[] { "Status", "FactorType" });

            migrationBuilder.CreateIndex(
                name: "IX_iam_mfa_factors_UserId_FactorType",
                schema: "iam",
                table: "iam_mfa_factors",
                columns: new[] { "UserId", "FactorType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iam_mfa_factors",
                schema: "iam");
        }
    }
}
