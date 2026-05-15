using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class AdminSearchTrigramIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_clients_DisplayName_trgm" ON crm.crm_clients USING gin ("DisplayName" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_clients_Notes_trgm" ON crm.crm_clients USING gin ("Notes" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_contact_persons_FirstName_trgm" ON crm.crm_contact_persons USING gin ("FirstName" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_contact_persons_LastName_trgm" ON crm.crm_contact_persons USING gin ("LastName" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_contact_persons_FullName_trgm" ON crm.crm_contact_persons USING gin ((("FirstName" || ' ' || COALESCE("LastName", ''))) gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_contact_persons_Notes_trgm" ON crm.crm_contact_persons USING gin ("Notes" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_contact_methods_DisplayValue_trgm" ON crm.crm_contact_methods USING gin ("DisplayValue" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_crm_contact_methods_NormalizedValue_trgm" ON crm.crm_contact_methods USING gin ("NormalizedValue" gin_trgm_ops);""");

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_pets_pets_Name_trgm" ON pets.pets USING gin ("Name" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_pets_pets_Notes_trgm" ON pets.pets USING gin ("Notes" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_pets_animal_types_Code_trgm" ON pets.animal_types USING gin ("Code" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_pets_animal_types_Name_trgm" ON pets.animal_types USING gin ("Name" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_pets_breeds_Code_trgm" ON pets.breeds USING gin ("Code" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_pets_breeds_Name_trgm" ON pets.breeds USING gin ("Name" gin_trgm_ops);""");

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_booking_requests_Channel_trgm" ON booking.booking_requests USING gin ("Channel" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_booking_requests_Status_trgm" ON booking.booking_requests USING gin ("Status" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_booking_requests_SelectionMode_trgm" ON booking.booking_requests USING gin ("SelectionMode" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_booking_requests_Notes_trgm" ON booking.booking_requests USING gin ("Notes" gin_trgm_ops);""");

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_appointments_Status_trgm" ON booking.appointments USING gin ("Status" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_appointments_CancellationReasonCode_trgm" ON booking.appointments USING gin ("CancellationReasonCode" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_appointments_CancellationNotes_trgm" ON booking.appointments USING gin ("CancellationNotes" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_appointment_items_ItemType_trgm" ON booking.appointment_items USING gin ("ItemType" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_appointment_items_OfferCodeSnapshot_trgm" ON booking.appointment_items USING gin ("OfferCodeSnapshot" gin_trgm_ops);""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_appointment_items_OfferDisplayNameSnapshot_trgm" ON booking.appointment_items USING gin ("OfferDisplayNameSnapshot" gin_trgm_ops);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_appointment_items_OfferDisplayNameSnapshot_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_appointment_items_OfferCodeSnapshot_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_appointment_items_ItemType_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_appointments_CancellationNotes_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_appointments_CancellationReasonCode_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_appointments_Status_trgm";""");

            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_booking_requests_Notes_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_booking_requests_SelectionMode_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_booking_requests_Status_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS booking."IX_booking_requests_Channel_trgm";""");

            migrationBuilder.Sql("""DROP INDEX IF EXISTS pets."IX_pets_breeds_Name_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS pets."IX_pets_breeds_Code_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS pets."IX_pets_animal_types_Name_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS pets."IX_pets_animal_types_Code_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS pets."IX_pets_pets_Notes_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS pets."IX_pets_pets_Name_trgm";""");

            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_contact_methods_NormalizedValue_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_contact_methods_DisplayValue_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_contact_persons_Notes_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_contact_persons_FullName_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_contact_persons_LastName_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_contact_persons_FirstName_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_clients_Notes_trgm";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS crm."IX_crm_clients_DisplayName_trgm";""");
        }
    }
}
