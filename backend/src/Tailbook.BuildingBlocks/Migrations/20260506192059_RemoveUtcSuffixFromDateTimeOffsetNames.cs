#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tailbook.BuildingBlocks.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUtcSuffixFromDateTimeOffsetNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "visitops",
                table: "visits",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "StartedAtUtc",
                schema: "visitops",
                table: "visits",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "visitops",
                table: "visits",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CompletedAtUtc",
                schema: "visitops",
                table: "visits",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "ClosedAtUtc",
                schema: "visitops",
                table: "visits",
                newName: "ClosedAt");

            migrationBuilder.RenameColumn(
                name: "CheckedInAtUtc",
                schema: "visitops",
                table: "visits",
                newName: "CheckedInAt");

            migrationBuilder.RenameIndex(
                name: "IX_visits_CheckedInAtUtc",
                schema: "visitops",
                table: "visits",
                newName: "IX_visits_CheckedInAt");

            migrationBuilder.RenameColumn(
                name: "RecordedAtUtc",
                schema: "visitops",
                table: "visit_skipped_components",
                newName: "RecordedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "visitops",
                table: "visit_price_adjustments",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_visit_price_adjustments_CreatedAtUtc",
                schema: "visitops",
                table: "visit_price_adjustments",
                newName: "IX_visit_price_adjustments_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "RecordedAtUtc",
                schema: "visitops",
                table: "visit_performed_procedures",
                newName: "RecordedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "visitops",
                table: "visit_execution_items",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "staff",
                table: "staff_working_schedules",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "staff",
                table: "staff_working_schedules",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "StartAtUtc",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "EndAtUtc",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "EndAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_staff_time_blocks_GroomerId_StartAtUtc_EndAtUtc",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "IX_staff_time_blocks_GroomerId_StartAt_EndAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "staff",
                table: "staff_groomers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "staff",
                table: "staff_groomers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "staff",
                table: "staff_groomer_capabilities",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "pricing_rules",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ValidToUtc",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "ValidTo");

            migrationBuilder.RenameColumn(
                name: "ValidFromUtc",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "ValidFrom");

            migrationBuilder.RenameColumn(
                name: "PublishedAtUtc",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "PublishedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_pricing_rule_sets_Status_ValidFromUtc",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "IX_pricing_rule_sets_Status_ValidFrom");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "booking",
                table: "price_snapshots",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_price_snapshots_SnapshotType_CreatedAtUtc",
                schema: "booking",
                table: "price_snapshots",
                newName: "IX_price_snapshots_SnapshotType_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_price_snapshots_CreatedAtUtc",
                schema: "booking",
                table: "price_snapshots",
                newName: "IX_price_snapshots_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "pets",
                table: "pets",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "pets",
                table: "pets",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "pets",
                table: "pet_photos",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ProcessedAtUtc",
                schema: "integration",
                table: "outbox_messages",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "OccurredAtUtc",
                schema: "integration",
                table: "outbox_messages",
                newName: "OccurredAt");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ProcessedAtUtc",
                schema: "integration",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ProcessedAt");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ModuleCode_OccurredAtUtc",
                schema: "integration",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ModuleCode_OccurredAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "notifications",
                table: "notification_templates",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "SentAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                newName: "SentAt");

            migrationBuilder.RenameColumn(
                name: "ProcessedAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "NextAttemptAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                newName: "NextAttemptAt");

            migrationBuilder.RenameColumn(
                name: "DeadLetteredAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                newName: "DeadLetteredAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_notification_jobs_NextAttemptAtUtc",
                schema: "notifications",
                table: "notification_jobs",
                newName: "IX_notification_jobs_NextAttemptAt");

            migrationBuilder.RenameColumn(
                name: "AttemptedAtUtc",
                schema: "notifications",
                table: "notification_delivery_attempts",
                newName: "AttemptedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "iam",
                table: "iam_users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "iam",
                table: "iam_users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "AssignedAtUtc",
                schema: "iam",
                table: "iam_role_assignments",
                newName: "AssignedAt");

            migrationBuilder.RenameColumn(
                name: "RevokedAtUtc",
                schema: "iam",
                table: "iam_refresh_tokens",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "ExpiresAtUtc",
                schema: "iam",
                table: "iam_refresh_tokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "iam",
                table: "iam_refresh_tokens",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UsedAtUtc",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "UsedAt");

            migrationBuilder.RenameColumn(
                name: "ExpiresAtUtc",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_iam_password_reset_tokens_UserId_ExpiresAtUtc",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "IX_iam_password_reset_tokens_UserId_ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "EnabledAtUtc",
                schema: "iam",
                table: "iam_mfa_factors",
                newName: "EnabledAt");

            migrationBuilder.RenameColumn(
                name: "DisabledAtUtc",
                schema: "iam",
                table: "iam_mfa_factors",
                newName: "DisabledAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "iam",
                table: "iam_mfa_factors",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "booking",
                table: "duration_snapshots",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_duration_snapshots_CreatedAtUtc",
                schema: "booking",
                table: "duration_snapshots",
                newName: "IX_duration_snapshots_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "duration_rules",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ValidToUtc",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "ValidTo");

            migrationBuilder.RenameColumn(
                name: "ValidFromUtc",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "ValidFrom");

            migrationBuilder.RenameColumn(
                name: "PublishedAtUtc",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "PublishedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_duration_rule_sets_Status_ValidFromUtc",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "IX_duration_rule_sets_Status_ValidFrom");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "crm",
                table: "crm_pet_contact_links",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "crm",
                table: "crm_pet_contact_links",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "crm",
                table: "crm_contact_persons",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "crm",
                table: "crm_contact_persons",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "crm",
                table: "crm_contact_methods",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "crm",
                table: "crm_contact_methods",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "crm",
                table: "crm_clients",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "crm",
                table: "crm_clients",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "catalog",
                table: "catalog_procedures",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "catalog_procedures",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "catalog",
                table: "catalog_offers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "catalog_offers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ValidToUtc",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "ValidTo");

            migrationBuilder.RenameColumn(
                name: "ValidFromUtc",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "ValidFrom");

            migrationBuilder.RenameColumn(
                name: "PublishedAtUtc",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "PublishedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "catalog",
                table: "catalog_offer_version_components",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "booking",
                table: "booking_requests",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "booking",
                table: "booking_requests",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_booking_requests_Status_CreatedAtUtc",
                schema: "booking",
                table: "booking_requests",
                newName: "IX_booking_requests_Status_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "booking",
                table: "booking_request_items",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "HappenedAtUtc",
                schema: "audit",
                table: "audit_entries",
                newName: "HappenedAt");

            migrationBuilder.RenameIndex(
                name: "IX_audit_entries_ModuleCode_EntityType_EntityId_HappenedAtUtc",
                schema: "audit",
                table: "audit_entries",
                newName: "IX_audit_entries_ModuleCode_EntityType_EntityId_HappenedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "booking",
                table: "appointments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "StartAtUtc",
                schema: "booking",
                table: "appointments",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "EndAtUtc",
                schema: "booking",
                table: "appointments",
                newName: "EndAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "booking",
                table: "appointments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CancelledAtUtc",
                schema: "booking",
                table: "appointments",
                newName: "CancelledAt");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_Status_StartAtUtc",
                schema: "booking",
                table: "appointments",
                newName: "IX_appointments_Status_StartAt");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_GroomerId_StartAtUtc_EndAtUtc",
                schema: "booking",
                table: "appointments",
                newName: "IX_appointments_GroomerId_StartAt_EndAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "booking",
                table: "appointment_items",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "HappenedAtUtc",
                schema: "audit",
                table: "access_audit_entries",
                newName: "HappenedAt");

            migrationBuilder.RenameIndex(
                name: "IX_access_audit_entries_ResourceType_ResourceId_HappenedAtUtc",
                schema: "audit",
                table: "access_audit_entries",
                newName: "IX_access_audit_entries_ResourceType_ResourceId_HappenedAt");

            migrationBuilder.RenameIndex(
                name: "IX_access_audit_entries_HappenedAtUtc",
                schema: "audit",
                table: "access_audit_entries",
                newName: "IX_access_audit_entries_HappenedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "visitops",
                table: "visits",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                schema: "visitops",
                table: "visits",
                newName: "StartedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "visitops",
                table: "visits",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                schema: "visitops",
                table: "visits",
                newName: "CompletedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ClosedAt",
                schema: "visitops",
                table: "visits",
                newName: "ClosedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CheckedInAt",
                schema: "visitops",
                table: "visits",
                newName: "CheckedInAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_visits_CheckedInAt",
                schema: "visitops",
                table: "visits",
                newName: "IX_visits_CheckedInAtUtc");

            migrationBuilder.RenameColumn(
                name: "RecordedAt",
                schema: "visitops",
                table: "visit_skipped_components",
                newName: "RecordedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "visitops",
                table: "visit_price_adjustments",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_visit_price_adjustments_CreatedAt",
                schema: "visitops",
                table: "visit_price_adjustments",
                newName: "IX_visit_price_adjustments_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "RecordedAt",
                schema: "visitops",
                table: "visit_performed_procedures",
                newName: "RecordedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "visitops",
                table: "visit_execution_items",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "staff",
                table: "staff_working_schedules",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "staff",
                table: "staff_working_schedules",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "StartAtUtc");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "EndAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_staff_time_blocks_GroomerId_StartAt_EndAt",
                schema: "staff",
                table: "staff_time_blocks",
                newName: "IX_staff_time_blocks_GroomerId_StartAtUtc_EndAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "staff",
                table: "staff_groomers",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "staff",
                table: "staff_groomers",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "staff",
                table: "staff_groomer_capabilities",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "pricing_rules",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ValidTo",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "ValidToUtc");

            migrationBuilder.RenameColumn(
                name: "ValidFrom",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "ValidFromUtc");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "PublishedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_pricing_rule_sets_Status_ValidFrom",
                schema: "catalog",
                table: "pricing_rule_sets",
                newName: "IX_pricing_rule_sets_Status_ValidFromUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "price_snapshots",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_price_snapshots_SnapshotType_CreatedAt",
                schema: "booking",
                table: "price_snapshots",
                newName: "IX_price_snapshots_SnapshotType_CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_price_snapshots_CreatedAt",
                schema: "booking",
                table: "price_snapshots",
                newName: "IX_price_snapshots_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "pets",
                table: "pets",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "pets",
                table: "pets",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "pets",
                table: "pet_photos",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                schema: "integration",
                table: "outbox_messages",
                newName: "ProcessedAtUtc");

            migrationBuilder.RenameColumn(
                name: "OccurredAt",
                schema: "integration",
                table: "outbox_messages",
                newName: "OccurredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ProcessedAt",
                schema: "integration",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ProcessedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_outbox_messages_ModuleCode_OccurredAt",
                schema: "integration",
                table: "outbox_messages",
                newName: "IX_outbox_messages_ModuleCode_OccurredAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "notifications",
                table: "notification_templates",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "SentAt",
                schema: "notifications",
                table: "notification_jobs",
                newName: "SentAtUtc");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                schema: "notifications",
                table: "notification_jobs",
                newName: "ProcessedAtUtc");

            migrationBuilder.RenameColumn(
                name: "NextAttemptAt",
                schema: "notifications",
                table: "notification_jobs",
                newName: "NextAttemptAtUtc");

            migrationBuilder.RenameColumn(
                name: "DeadLetteredAt",
                schema: "notifications",
                table: "notification_jobs",
                newName: "DeadLetteredAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "notifications",
                table: "notification_jobs",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_notification_jobs_NextAttemptAt",
                schema: "notifications",
                table: "notification_jobs",
                newName: "IX_notification_jobs_NextAttemptAtUtc");

            migrationBuilder.RenameColumn(
                name: "AttemptedAt",
                schema: "notifications",
                table: "notification_delivery_attempts",
                newName: "AttemptedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "iam",
                table: "iam_users",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "iam",
                table: "iam_users",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "AssignedAt",
                schema: "iam",
                table: "iam_role_assignments",
                newName: "AssignedAtUtc");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "iam",
                table: "iam_refresh_tokens",
                newName: "RevokedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "iam",
                table: "iam_refresh_tokens",
                newName: "ExpiresAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "iam",
                table: "iam_refresh_tokens",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UsedAt",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "UsedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "ExpiresAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_iam_password_reset_tokens_UserId_ExpiresAt",
                schema: "iam",
                table: "iam_password_reset_tokens",
                newName: "IX_iam_password_reset_tokens_UserId_ExpiresAtUtc");

            migrationBuilder.RenameColumn(
                name: "EnabledAt",
                schema: "iam",
                table: "iam_mfa_factors",
                newName: "EnabledAtUtc");

            migrationBuilder.RenameColumn(
                name: "DisabledAt",
                schema: "iam",
                table: "iam_mfa_factors",
                newName: "DisabledAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "iam",
                table: "iam_mfa_factors",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "duration_snapshots",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_duration_snapshots_CreatedAt",
                schema: "booking",
                table: "duration_snapshots",
                newName: "IX_duration_snapshots_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "duration_rules",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ValidTo",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "ValidToUtc");

            migrationBuilder.RenameColumn(
                name: "ValidFrom",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "ValidFromUtc");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "PublishedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_duration_rule_sets_Status_ValidFrom",
                schema: "catalog",
                table: "duration_rule_sets",
                newName: "IX_duration_rule_sets_Status_ValidFromUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "crm",
                table: "crm_pet_contact_links",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "crm",
                table: "crm_pet_contact_links",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "crm",
                table: "crm_contact_persons",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "crm",
                table: "crm_contact_persons",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "crm",
                table: "crm_contact_methods",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "crm",
                table: "crm_contact_methods",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "crm",
                table: "crm_clients",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "crm",
                table: "crm_clients",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "catalog_procedures",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "catalog_procedures",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "catalog_offers",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "catalog_offers",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ValidTo",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "ValidToUtc");

            migrationBuilder.RenameColumn(
                name: "ValidFrom",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "ValidFromUtc");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "PublishedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "catalog_offer_versions",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "catalog_offer_version_components",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "booking",
                table: "booking_requests",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "booking_requests",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_booking_requests_Status_CreatedAt",
                schema: "booking",
                table: "booking_requests",
                newName: "IX_booking_requests_Status_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "booking_request_items",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "HappenedAt",
                schema: "audit",
                table: "audit_entries",
                newName: "HappenedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_audit_entries_ModuleCode_EntityType_EntityId_HappenedAt",
                schema: "audit",
                table: "audit_entries",
                newName: "IX_audit_entries_ModuleCode_EntityType_EntityId_HappenedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "booking",
                table: "appointments",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                schema: "booking",
                table: "appointments",
                newName: "StartAtUtc");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                schema: "booking",
                table: "appointments",
                newName: "EndAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "appointments",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CancelledAt",
                schema: "booking",
                table: "appointments",
                newName: "CancelledAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_Status_StartAt",
                schema: "booking",
                table: "appointments",
                newName: "IX_appointments_Status_StartAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_appointments_GroomerId_StartAt_EndAt",
                schema: "booking",
                table: "appointments",
                newName: "IX_appointments_GroomerId_StartAtUtc_EndAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "booking",
                table: "appointment_items",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "HappenedAt",
                schema: "audit",
                table: "access_audit_entries",
                newName: "HappenedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_access_audit_entries_ResourceType_ResourceId_HappenedAt",
                schema: "audit",
                table: "access_audit_entries",
                newName: "IX_access_audit_entries_ResourceType_ResourceId_HappenedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_access_audit_entries_HappenedAt",
                schema: "audit",
                table: "access_audit_entries",
                newName: "IX_access_audit_entries_HappenedAtUtc");
        }
    }
}
