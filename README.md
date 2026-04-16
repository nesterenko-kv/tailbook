# Tailbook

Tailbook as a modular monolith + monorepo foundation.

## Backend auth quickstart (dev token)

When testing protected backend endpoints locally:

1. Issue a development token:

```bash
curl -k -X POST https://localhost:5001/api/identity/dev-token \
  -H "Content-Type: application/json" \
  -d '{
    "SubjectId": "dev-user-1",
    "Email": "dev@example.com",
    "Roles": ["Admin"]
  }'
```

1. Call protected endpoints with the token in the `Authorization` header:

```bash
curl -k https://localhost:5001/api/identity/me \
  -H "Authorization: Bearer <accessToken>"
```

If you get `401` while using a valid token, make sure you call `https://localhost:5001` directly. Calling `http://localhost:5000` may redirect to HTTPS and some clients drop the `Authorization` header on redirect.

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

# 5) apply latest db migration (from backend/src/Tailbook.Api.Host)
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

## Database & migrations

- **Postgres / pgAdmin (dev via `docker-compose`)**
  - Postgres and pgAdmin are started with `docker compose up -d` from the repo root (uses `docker-compose.yml`).
  - Once containers are up, open pgAdmin in your browser at `http://localhost:5050`.
  - Default pgAdmin credentials (see `docker-compose.yml` to confirm):
    - **Email**: `admin@admin.com`
    - **Password**: `admin`
  - After logging in:
    - Right‑click **Servers → Create → Server...**
    - On the **General** tab set **Name** to something like `Tailbook Local`.
    - On the **Connection** tab use:
      - **Host**: `db`
      - **Port**: `5432`
      - **Maintenance database**: `tailbook`
      - **Username**: `postgres`
      - **Password**: `postgres`
    - Save and you should see the `tailbook` database under the new server.
- **Managing EF Core migrations**
  - **Add a new migration** (run from repo root):
    ```bash
    dotnet ef migrations add Init --project src/Tailbook.BuildingBlocks/Tailbook.BuildingBlocks.csproj --startup-project src/Tailbook.Api.Host/Tailbook.Api.Host.csproj
    ```
  - **Apply migrations to the local database** (same as step 5 above):
    ```bash
    dotnet ef database update \
      --project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj \
      --startup-project backend/src/Tailbook.Api.Host/Tailbook.Api.Host.csproj
    ```
  - Make sure the Postgres container is running before running EF commands.

