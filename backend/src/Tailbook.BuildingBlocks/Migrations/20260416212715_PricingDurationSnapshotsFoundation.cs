using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class PricingDurationSnapshotsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "duration_rule_sets",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duration_rule_sets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "duration_snapshots",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceMinutes = table.Column<int>(type: "integer", nullable: false),
                    ReservedMinutes = table.Column<int>(type: "integer", nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duration_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "price_snapshots",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rule_sets",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_rule_sets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "duration_rules",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SpecificityScore = table.Column<int>(type: "integer", nullable: false),
                    BaseMinutes = table.Column<int>(type: "integer", nullable: false),
                    BufferBeforeMinutes = table.Column<int>(type: "integer", nullable: false),
                    BufferAfterMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duration_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_duration_rules_catalog_offers_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "catalog",
                        principalTable: "catalog_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_duration_rules_duration_rule_sets_RuleSetId",
                        column: x => x.RuleSetId,
                        principalSchema: "catalog",
                        principalTable: "duration_rule_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "duration_snapshot_lines",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Minutes = table.Column<int>(type: "integer", nullable: false),
                    SourceRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    SequenceNo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duration_snapshot_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_duration_snapshot_lines_duration_snapshots_DurationSnapshot~",
                        column: x => x.DurationSnapshotId,
                        principalSchema: "booking",
                        principalTable: "duration_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "price_snapshot_lines",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    SequenceNo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_snapshot_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_snapshot_lines_price_snapshots_PriceSnapshotId",
                        column: x => x.PriceSnapshotId,
                        principalSchema: "booking",
                        principalTable: "price_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rules",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SpecificityScore = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pricing_rules_catalog_offers_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "catalog",
                        principalTable: "catalog_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pricing_rules_pricing_rule_sets_RuleSetId",
                        column: x => x.RuleSetId,
                        principalSchema: "catalog",
                        principalTable: "pricing_rule_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "duration_rule_conditions",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DurationRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    BreedId = table.Column<Guid>(type: "uuid", nullable: true),
                    BreedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoatTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SizeCategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duration_rule_conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_duration_rule_conditions_duration_rules_DurationRuleId",
                        column: x => x.DurationRuleId,
                        principalSchema: "catalog",
                        principalTable: "duration_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rule_conditions",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    BreedId = table.Column<Guid>(type: "uuid", nullable: true),
                    BreedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoatTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SizeCategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_rule_conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pricing_rule_conditions_pricing_rules_PriceRuleId",
                        column: x => x.PriceRuleId,
                        principalSchema: "catalog",
                        principalTable: "pricing_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_duration_rule_conditions_AnimalTypeId_BreedId_BreedGroupId_~",
                schema: "catalog",
                table: "duration_rule_conditions",
                columns: new[] { "AnimalTypeId", "BreedId", "BreedGroupId", "CoatTypeId", "SizeCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_duration_rule_conditions_DurationRuleId",
                schema: "catalog",
                table: "duration_rule_conditions",
                column: "DurationRuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_duration_rule_sets_Status_ValidFromUtc",
                schema: "catalog",
                table: "duration_rule_sets",
                columns: new[] { "Status", "ValidFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_duration_rule_sets_VersionNo",
                schema: "catalog",
                table: "duration_rule_sets",
                column: "VersionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_duration_rules_OfferId",
                schema: "catalog",
                table: "duration_rules",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_duration_rules_RuleSetId_OfferId_Priority",
                schema: "catalog",
                table: "duration_rules",
                columns: new[] { "RuleSetId", "OfferId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_duration_snapshot_lines_DurationSnapshotId_SequenceNo",
                schema: "booking",
                table: "duration_snapshot_lines",
                columns: new[] { "DurationSnapshotId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_duration_snapshots_CreatedAtUtc",
                schema: "booking",
                table: "duration_snapshots",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_price_snapshot_lines_PriceSnapshotId_SequenceNo",
                schema: "booking",
                table: "price_snapshot_lines",
                columns: new[] { "PriceSnapshotId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_snapshots_CreatedAtUtc",
                schema: "booking",
                table: "price_snapshots",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_price_snapshots_SnapshotType_CreatedAtUtc",
                schema: "booking",
                table: "price_snapshots",
                columns: new[] { "SnapshotType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rule_conditions_AnimalTypeId_BreedId_BreedGroupId_C~",
                schema: "catalog",
                table: "pricing_rule_conditions",
                columns: new[] { "AnimalTypeId", "BreedId", "BreedGroupId", "CoatTypeId", "SizeCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rule_conditions_PriceRuleId",
                schema: "catalog",
                table: "pricing_rule_conditions",
                column: "PriceRuleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rule_sets_Status_ValidFromUtc",
                schema: "catalog",
                table: "pricing_rule_sets",
                columns: new[] { "Status", "ValidFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rule_sets_VersionNo",
                schema: "catalog",
                table: "pricing_rule_sets",
                column: "VersionNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_OfferId",
                schema: "catalog",
                table: "pricing_rules",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_RuleSetId_OfferId_Priority",
                schema: "catalog",
                table: "pricing_rules",
                columns: new[] { "RuleSetId", "OfferId", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duration_rule_conditions",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "duration_snapshot_lines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "price_snapshot_lines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "pricing_rule_conditions",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "duration_rules",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "duration_snapshots",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "price_snapshots",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "pricing_rules",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "duration_rule_sets",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_rule_sets",
                schema: "catalog");
        }
    }
}
