# Tailbook

Tailbook is a modular monolith + monorepo grooming salon platform.

## Repo shape

- `backend/src/Tailbook.Api.Host` - API host
- `backend/src/Tailbook.BuildingBlocks` - shared infra + EF Core + outbox
- `backend/src/Tailbook.Modules.*` - business modules by bounded context
- `apps/admin-web` - admin web app
- `apps/client-web` - client portal web app
- `apps/groomer-web` - groomer web app
- `docs/adr` - architecture decision records
- `ops/runbooks` - operational runbooks
- `ops/scripts` - backup/restore helper scripts

## Development quickstart

```bash
cp .env.example .env

docker compose up -d
pnpm install

dotnet restore backend/Tailbook.slnx

dotnet ef database update \
  --project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj \
  --startup-project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj

dotnet watch --project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj run
pnpm dev:admin
pnpm dev:client
pnpm dev:groomer

dotnet test backend/Tailbook.slnx
```

## Production-like local stack

```bash
cp .env.example .env

docker compose -f docker-compose.production.yml up --build -d
```

Published local endpoints:
- API: `http://localhost:5001`
- Admin web: `http://localhost:3001`
- Client web: `http://localhost:3002`
- Groomer web: `http://localhost:3003`
- Health live: `http://localhost:5001/health/live`
- Health ready: `http://localhost:5001/health/ready`

## Database commands

Apply migrations:

```bash
dotnet ef database update \
  --project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj \
  --startup-project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj
```

Create a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project backend/src/Tailbook.BuildingBlocks/Tailbook.BuildingBlocks.csproj \
  --startup-project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj
```

## Operations docs

- `ops/runbooks/local-production.md`
- `ops/runbooks/backup-restore.md`
- `ops/runbooks/seed-import-strategy.md`
- `docs/final-repo-map.md`
- `docs/adr/README.md`
