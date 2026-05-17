# Tailbook

![CI](https://github.com/nesterenko-kv/tailbook/actions/workflows/ci.yml/badge.svg)
[![codecov](https://codecov.io/gh/nesterenko-kv/tailbook/branch/main/graph/badge.svg)](https://codecov.io/gh/nesterenko-kv/tailbook)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Node](https://img.shields.io/badge/Node-22-5FA04E?logo=node.js)
![Trivy](https://img.shields.io/badge/Trivy-Container%20Scanning-1904DA)

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

## Testing

Run all backend and frontend tests with coverage:

```bash
# .NET backend tests with code coverage
dotnet test backend/Tailbook.slnx --collect:"XPlat Code Coverage"

# pnpm frontend tests with coverage
pnpm test -- --coverage
```

Coverage reports are generated as HTML artifacts in CI — downloadable from the **Summary** tab of each workflow run.
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
- Redis: `localhost:6379` in the developer Compose stack
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
