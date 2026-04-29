# Security Permissions

Date: 2026-04-29

## Permission Naming

Permission codes use lowercase dot-separated scopes:

- `<module>.<resource>.<action>` for resource-specific permissions.
- `app.<surface>.access` for frontend surface access.
- Client/groomer portal permissions are scoped to their own surfaces and should not grant admin routes.

## Sensitive Admin Permissions

- `iam.roles.assign` is required to assign roles and is also required when creating a user with initial roles.
- `notifications.write` is required to process the notification outbox.
- `visit.adjustments.write` is required for visit financial adjustments.
- `visit.write` remains scoped to operational visit execution such as check-in, performed procedures, skipped components, completion, and close.

## Current Follow-Up

Fine-grained per-entity scopes are still basic. Future work should add explicit entity ownership or salon-location scopes when Tailbook supports multi-location tenancy or delegated managers.
