using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPetsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pets");

            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.CreateTable(
                name: "animal_types",
                schema: "pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_animal_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crm_clients",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "breed_groups",
                schema: "pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_breed_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_breed_groups_animal_types_AnimalTypeId",
                        column: x => x.AnimalTypeId,
                        principalSchema: "pets",
                        principalTable: "animal_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "coat_types",
                schema: "pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coat_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_coat_types_animal_types_AnimalTypeId",
                        column: x => x.AnimalTypeId,
                        principalSchema: "pets",
                        principalTable: "animal_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "size_categories",
                schema: "pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MinWeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MaxWeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_size_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_size_categories_animal_types_AnimalTypeId",
                        column: x => x.AnimalTypeId,
                        principalSchema: "pets",
                        principalTable: "animal_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crm_contact_persons",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TrustLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_contact_persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_contact_persons_crm_clients_ClientId",
                        column: x => x.ClientId,
                        principalSchema: "crm",
                        principalTable: "crm_clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "breeds",
                schema: "pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BreedGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_breeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_breeds_animal_types_AnimalTypeId",
                        column: x => x.AnimalTypeId,
                        principalSchema: "pets",
                        principalTable: "animal_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_breeds_breed_groups_BreedGroupId",
                        column: x => x.BreedGroupId,
                        principalSchema: "pets",
                        principalTable: "breed_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crm_contact_methods",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NormalizedValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_contact_methods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_contact_methods_crm_contact_persons_ContactPersonId",
                        column: x => x.ContactPersonId,
                        principalSchema: "crm",
                        principalTable: "crm_contact_persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crm_pet_contact_links",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleCodes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CanPickUp = table.Column<bool>(type: "boolean", nullable: false),
                    CanPay = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivesNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_pet_contact_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_pet_contact_links_crm_contact_persons_ContactPersonId",
                        column: x => x.ContactPersonId,
                        principalSchema: "crm",
                        principalTable: "crm_contact_persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pets",
                schema: "pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AnimalTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BreedId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoatTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SizeCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pets_animal_types_AnimalTypeId",
                        column: x => x.AnimalTypeId,
                        principalSchema: "pets",
                        principalTable: "animal_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pets_breeds_BreedId",
                        column: x => x.BreedId,
                        principalSchema: "pets",
                        principalTable: "breeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pets_coat_types_CoatTypeId",
                        column: x => x.CoatTypeId,
                        principalSchema: "pets",
                        principalTable: "coat_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pets_size_categories_SizeCategoryId",
                        column: x => x.SizeCategoryId,
                        principalSchema: "pets",
                        principalTable: "size_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pet_photos",
                schema: "pets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PetId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pet_photos_pets_PetId",
                        column: x => x.PetId,
                        principalSchema: "pets",
                        principalTable: "pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_animal_types_Code",
                schema: "pets",
                table: "animal_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_breed_groups_AnimalTypeId_Code",
                schema: "pets",
                table: "breed_groups",
                columns: new[] { "AnimalTypeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_breeds_AnimalTypeId_Code",
                schema: "pets",
                table: "breeds",
                columns: new[] { "AnimalTypeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_breeds_BreedGroupId",
                schema: "pets",
                table: "breeds",
                column: "BreedGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_coat_types_AnimalTypeId",
                schema: "pets",
                table: "coat_types",
                column: "AnimalTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_coat_types_Code",
                schema: "pets",
                table: "coat_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crm_clients_DisplayName",
                schema: "crm",
                table: "crm_clients",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_crm_clients_Status",
                schema: "crm",
                table: "crm_clients",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_crm_contact_methods_ContactPersonId",
                schema: "crm",
                table: "crm_contact_methods",
                column: "ContactPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_contact_methods_ContactPersonId_MethodType_NormalizedVa~",
                schema: "crm",
                table: "crm_contact_methods",
                columns: new[] { "ContactPersonId", "MethodType", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crm_contact_persons_ClientId",
                schema: "crm",
                table: "crm_contact_persons",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_pet_contact_links_ContactPersonId",
                schema: "crm",
                table: "crm_pet_contact_links",
                column: "ContactPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_pet_contact_links_PetId",
                schema: "crm",
                table: "crm_pet_contact_links",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_pet_contact_links_PetId_ContactPersonId",
                schema: "crm",
                table: "crm_pet_contact_links",
                columns: new[] { "PetId", "ContactPersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pet_photos_PetId",
                schema: "pets",
                table: "pet_photos",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_AnimalTypeId",
                schema: "pets",
                table: "pets",
                column: "AnimalTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_BreedId",
                schema: "pets",
                table: "pets",
                column: "BreedId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_ClientId",
                schema: "pets",
                table: "pets",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_CoatTypeId",
                schema: "pets",
                table: "pets",
                column: "CoatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_pets_Name",
                schema: "pets",
                table: "pets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_pets_SizeCategoryId",
                schema: "pets",
                table: "pets",
                column: "SizeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_size_categories_AnimalTypeId",
                schema: "pets",
                table: "size_categories",
                column: "AnimalTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_size_categories_Code",
                schema: "pets",
                table: "size_categories",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_contact_methods",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "crm_pet_contact_links",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pet_photos",
                schema: "pets");

            migrationBuilder.DropTable(
                name: "crm_contact_persons",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pets",
                schema: "pets");

            migrationBuilder.DropTable(
                name: "crm_clients",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "breeds",
                schema: "pets");

            migrationBuilder.DropTable(
                name: "coat_types",
                schema: "pets");

            migrationBuilder.DropTable(
                name: "size_categories",
                schema: "pets");

            migrationBuilder.DropTable(
                name: "breed_groups",
                schema: "pets");

            migrationBuilder.DropTable(
                name: "animal_types",
                schema: "pets");
        }
    }
}
