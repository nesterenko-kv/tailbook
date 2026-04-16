# ADR 0001 - Stage 0 foundation

## Status
Accepted

## Decision
Start Tailbook as:
- a modular monolith backend on .NET 10
- a pnpm + Turborepo monorepo
- PostgreSQL via Docker Compose
- three separate Next.js web shells for admin, client, and groomer
- a single outbox table from day one
