# Tailbook Agent Guide

## Repository Layout

- `backend/Tailbook.slnx` is the .NET solution.
- `backend/src/Tailbook.Api.Host` hosts the API, auth, health checks, startup validation, Swagger, and module wiring.
- `backend/src/Tailbook.BuildingBlocks` contains shared infrastructure, EF Core persistence, auth helpers, outbox support, and cross-module abstractions.
- `backend/src/Tailbook.Modules.*` contains bounded-context modules: Identity, Customer, Pets, Catalog, Booking, VisitOperations, Staff, Notifications, Audit, and Reporting.
- `backend/src/Tailbook.SharedKernel` contains stable shared abstractions such as clocks.
- `backend/tests/Tailbook.Api.Tests` contains API integration and flow tests.
- `backend/tests/Tailbook.Architecture.Tests` contains module boundary tests.
- `apps/admin-web`, `apps/client-web`, and `apps/groomer-web` are Next.js applications.
- `packages/typescript-config` contains shared TypeScript config.
- `docs/adr`, `docs/reviews`, and `ops/runbooks` hold architecture, review, and operational guidance.

## Build, Test, And Lint Commands

Backend:

```bash
dotnet restore backend/Tailbook.slnx
dotnet build backend/Tailbook.slnx --no-restore
dotnet test backend/Tailbook.slnx --no-build
```

Frontend:

```bash
pnpm install
pnpm lint
pnpm typecheck
pnpm build
```

Docker/config:

```bash
docker compose config
docker compose -f docker-compose.production.yml config
```

Useful scoped frontend commands:

```bash
pnpm --filter admin-web lint
pnpm --filter admin-web typecheck
pnpm --filter client-web lint
pnpm --filter client-web typecheck
pnpm --filter groomer-web lint
pnpm --filter groomer-web typecheck
```

## Coding Conventions

- Follow existing FastEndpoints, EF Core, and module registration patterns.
- Keep endpoint request and response contracts explicit and typed.
- Prefer FluentValidation for request validation where the endpoint already follows that style.
- Return clear validation or conflict responses for invalid user or domain actions.
- Keep frontend calls behind typed API helpers and `lib/types.ts` contracts.
- Preserve the existing Next.js app structure and operational UI style.
- Add focused tests for every behavior change.
- Update docs and runbooks when behavior, operations, or security posture changes.

## Module Boundary Rules

- Modules must not reference other module assemblies directly.
- Cross-module reads and actions must go through `Tailbook.BuildingBlocks.Abstractions` interfaces.
- Shared infrastructure belongs in `Tailbook.BuildingBlocks` only when more than one module needs it.
- Domain rules belong in the owning module, not in the API host or frontend.
- The API host wires modules together; it should not contain business behavior.

## Security Rules

- Never commit real credentials, tokens, secrets, or local production keys.
- Do not log auth headers, passwords, refresh tokens, reset tokens, or request bodies containing secrets.
- Use hashed persisted tokens for long-lived authentication or recovery flows.
- Avoid user enumeration in public identity flows.
- Ensure admin endpoints check named permissions, not only authentication.
- Record audit or access-audit entries for sensitive reads and mutations.
- Keep local production-like Docker guidance explicit about HTTP-only limitations unless TLS is added externally.

## Definition Of Done

- The increment is small, reviewable, and scoped to the requested behavior.
- Backend and frontend contracts are synchronized.
- Tests are added or updated for behavior changes.
- Relevant scoped verification commands have been run and results are documented.
- Full backend and frontend verification is run before final handoff.
- Known limitations are documented in `ITERATION_LOG.md` or the relevant docs.
