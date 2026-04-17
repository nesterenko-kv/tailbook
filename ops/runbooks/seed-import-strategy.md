# Seed and import strategy

## Seed data that should remain code-driven
- IAM system roles and permissions
- Bootstrap admin account
- Notification templates
- Core pet taxonomy needed for MVP flows

## Import data that should come from salon-owned files
- CRM clients and contact persons
- Contact methods and per-pet role links
- Pets and optional pet photos
- Catalog offers, versions, and components if migrating from an older system
- Price and duration rule tables if a reliable digital source exists

## Import sequence
1. Pet taxonomy reference data
2. IAM bootstrap + admin users
3. CRM clients and contacts
4. Pets and pet links
5. Catalog offers / procedures / versions
6. Pricing and duration rule sets
7. Historical booking / appointments / visits (optional, only after mapping quality review)

## Guardrails
- Import through dedicated scripts/jobs, not manual SQL edits in production.
- Keep historical snapshots immutable once imported.
- Record import provenance (`source file`, `imported at`, `import batch id`) in future migration/import tooling.
