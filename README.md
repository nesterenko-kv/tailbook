# Tailbook

Tailbook as a modular monolith + monorepo foundation.

## Apps
- `backend/src/Tailbook.Api.Host` - API host
- `apps/admin-web` - admin UI shell
- `apps/client-web` - client portal shell
- `apps/groomer-web` - groomer UI shell

## Commands to run locally

```bash
# 1) copy env
cp .env.example .env

# 2) start postgres + pgadmin
docker compose up -d

# 3) install frontend workspace deps
pnpm install

# 4) restore backend deps
dotnet restore backend/Tailbook.sln

# 5) apply migration
dotnet ef database update \
  --project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj \
  --startup-project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj

# 6) run api
dotnet watch --project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj run

# 7) run web shells
pnpm dev:admin
pnpm dev:client
pnpm dev:groomer

# 8) run backend tests
dotnet test backend/Tailbook.sln
```

