using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class BreedAllowedCoatTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "breed_allowed_coat_types",
                schema: "pets",
                columns: table => new
                {
                    BreedId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoatTypeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_breed_allowed_coat_types", x => new { x.BreedId, x.CoatTypeId });
                    table.ForeignKey(
                        name: "FK_breed_allowed_coat_types_breeds_BreedId",
                        column: x => x.BreedId,
                        principalSchema: "pets",
                        principalTable: "breeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_breed_allowed_coat_types_coat_types_CoatTypeId",
                        column: x => x.CoatTypeId,
                        principalSchema: "pets",
                        principalTable: "coat_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_breed_allowed_coat_types_CoatTypeId",
                schema: "pets",
                table: "breed_allowed_coat_types",
                column: "CoatTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "breed_allowed_coat_types",
                schema: "pets");
        }
    }
}
