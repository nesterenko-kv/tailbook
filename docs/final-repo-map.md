# Final repo map

## Root
- `apps/` - web applications
- `backend/` - .NET backend solution
- `docs/` - architecture and review docs
- `ops/` - operational scripts and runbooks
- `packages/` - shared workspace packages

## Apps
- `apps/admin-web` - admin UI
- `apps/client-web` - client portal
- `apps/groomer-web` - groomer-safe UI

## Backend
- `Tailbook.Api.Host` - ASP.NET Core host, auth, health, startup, Swagger
- `Tailbook.BuildingBlocks` - shared persistence, outbox, auth helpers, cross-module abstractions
- `Tailbook.Modules.Identity` - identity, roles, permissions, sessions, password reset, MFA
- `Tailbook.Modules.Customer` - clients, contacts, contact preferences, pet contact links
- `Tailbook.Modules.Pets` - pets, pet photos, taxonomy, breed compatibility
- `Tailbook.Modules.Catalog` - offers, versions, procedures, pricing and duration rules
- `Tailbook.Modules.Booking` - booking requests, appointments, quote previews, snapshots
- `Tailbook.Modules.VisitOperations` - visits, execution items, performed/skipped work, adjustments
- `Tailbook.Modules.Staff` - groomers, capabilities, schedules, availability
- `Tailbook.Modules.Notifications` - notification jobs, templates, outbox processing
- `Tailbook.Modules.Audit` - audit trail and access audit records
- `Tailbook.Modules.Reporting` - reporting read models and calculations
- `Tailbook.SharedKernel`

## Module internal layout
Each `Tailbook.Modules.*` project remains intact and uses the same internal shape where applicable:

- `<ModuleName>Module.cs` - module registration and persistence model registration
- `Api/Admin`, `Api/Client`, `Api/Groomer`, `Api/Public` - endpoint classes and HTTP contracts
- `Application/Abstractions` - module-owned ports
- `Application/Common` - validation/errors shared by use cases
- `Application/<Feature>/Commands|Queries|Models` - use-case interfaces, DTOs, and application-facing models
- `Domain/Aggregates`, `Domain/Entities`, `Domain/ValueObjects` - framework-free business model
- `Infrastructure/Persistence/Configurations` - EF Core mappings
- `Infrastructure/Services`, `Infrastructure/Options`, `Infrastructure/BackgroundJobs`, `Infrastructure/Seeding` - technical implementations
- `Contracts` - existing stable constants/contracts

Module `GlobalUsings.cs` files intentionally avoid Infrastructure namespaces. `<ModuleName>Module.cs` files import Infrastructure explicitly to wire implementations to Application interfaces and BuildingBlocks abstractions.

## Operations
- `ops/runbooks/local-production.md`
- `ops/runbooks/backup-restore.md`
- `ops/runbooks/seed-import-strategy.md`
- `ops/scripts/*.sh` and `*.ps1`

## Deployment assets
- `docker-compose.yml` - developer database stack
- `docker-compose.production.yml` - production-like local stack
- `backend/src/Tailbook.Api.Host/Dockerfile`
- `apps/*/Dockerfile`
