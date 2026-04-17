using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class BreedAllowedSizeCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "breed_allowed_size_categories",
                schema: "pets",
                columns: table => new
                {
                    BreedId = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_breed_allowed_size_categories", x => new { x.BreedId, x.SizeCategoryId });
                    table.ForeignKey(
                        name: "FK_breed_allowed_size_categories_breeds_BreedId",
                        column: x => x.BreedId,
                        principalSchema: "pets",
                        principalTable: "breeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_breed_allowed_size_categories_size_categories_SizeCategoryId",
                        column: x => x.SizeCategoryId,
                        principalSchema: "pets",
                        principalTable: "size_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_breed_allowed_size_categories_SizeCategoryId",
                schema: "pets",
                table: "breed_allowed_size_categories",
                column: "SizeCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "breed_allowed_size_categories",
                schema: "pets");
        }
    }
}
