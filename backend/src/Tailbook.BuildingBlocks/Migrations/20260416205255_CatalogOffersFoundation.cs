using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class CatalogOffersFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "catalog_offers",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OfferType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_offers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_procedures",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_procedures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_offer_versions",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PolicyText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ChangeNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_offer_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_offer_versions_catalog_offers_OfferId",
                        column: x => x.OfferId,
                        principalSchema: "catalog",
                        principalTable: "catalog_offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_offer_version_components",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcedureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SequenceNo = table.Column<int>(type: "integer", nullable: false),
                    DefaultExpected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_offer_version_components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_offer_version_components_catalog_offer_versions_Off~",
                        column: x => x.OfferVersionId,
                        principalSchema: "catalog",
                        principalTable: "catalog_offer_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_offer_version_components_catalog_procedures_Procedu~",
                        column: x => x.ProcedureId,
                        principalSchema: "catalog",
                        principalTable: "catalog_procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offer_version_components_OfferVersionId_ProcedureId",
                schema: "catalog",
                table: "catalog_offer_version_components",
                columns: new[] { "OfferVersionId", "ProcedureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offer_version_components_OfferVersionId_SequenceNo",
                schema: "catalog",
                table: "catalog_offer_version_components",
                columns: new[] { "OfferVersionId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offer_version_components_ProcedureId",
                schema: "catalog",
                table: "catalog_offer_version_components",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offer_versions_OfferId_Status",
                schema: "catalog",
                table: "catalog_offer_versions",
                columns: new[] { "OfferId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offer_versions_OfferId_VersionNo",
                schema: "catalog",
                table: "catalog_offer_versions",
                columns: new[] { "OfferId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offers_Code",
                schema: "catalog",
                table: "catalog_offers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_offers_OfferType_DisplayName",
                schema: "catalog",
                table: "catalog_offers",
                columns: new[] { "OfferType", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_procedures_Code",
                schema: "catalog",
                table: "catalog_procedures",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_procedures_Name",
                schema: "catalog",
                table: "catalog_procedures",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_offer_version_components",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_offer_versions",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_procedures",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_offers",
                schema: "catalog");
        }
    }
}
