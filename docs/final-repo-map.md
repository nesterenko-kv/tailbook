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
- `Tailbook.Modules.Identity`
- `Tailbook.Modules.Customer`
- `Tailbook.Modules.Pets`
- `Tailbook.Modules.Catalog`
- `Tailbook.Modules.Booking`
- `Tailbook.Modules.VisitOperations`
- `Tailbook.Modules.Staff`
- `Tailbook.Modules.Notifications`
- `Tailbook.Modules.Audit`
- `Tailbook.Modules.Reporting`
- `Tailbook.SharedKernel`

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
