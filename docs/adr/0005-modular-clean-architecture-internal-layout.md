# ADR 0005: Modular Clean Architecture internal layout

Date: 2026-04-30
Status: Accepted

## Context

Tailbook remains a modular monolith. Each bounded-context module stays as one project so deployment, composition, EF migrations, and API discovery remain centralized in `Tailbook.Api.Host` and `Tailbook.BuildingBlocks`.

The module projects had the correct top-level folders, but many files were flat under `Application`, `Domain`, and `Infrastructure`. Some modules also had direct project references to Identity only to reuse permission constants.

## Decision

Each `Tailbook.Modules.*` project now uses consistent internal Clean Architecture folders:

- `Api/` contains FastEndpoints endpoints and HTTP request/response contracts.
- `Application/` contains use-case contracts, DTOs, commands, validation helpers, and ports.
- `Domain/` contains pure aggregates, entities, and value objects.
- `Infrastructure/` contains EF Core model configuration, persistence-backed services, options, seeders, background services, local filesystem sinks, and technical command handlers.
- `Contracts/` remains for stable module-public constants and DTOs where already needed.

Module-to-module project references are not allowed. Cross-module runtime needs use `Tailbook.BuildingBlocks.Abstractions`, and shared permission constants used outside Identity are exposed from `Tailbook.BuildingBlocks.Abstractions.Security`.

## Consequences

- Existing module projects and endpoint behavior remain intact.
- EF Core mappings live under `Infrastructure/Persistence/Configurations`.
- API endpoints depend on Application query/service interfaces for module use cases, not Infrastructure concrete services or `AppDbContext`.
- Module-wide `GlobalUsings.cs` files do not import Infrastructure namespaces; module composition files import Infrastructure explicitly where they wire implementations.
- Application source is guarded from Infrastructure, API, EF Core, ASP.NET, and FastEndpoints references.
- Domain source is guarded from Application, Infrastructure, API, EF Core, ASP.NET, and FastEndpoints references.
- Architecture tests enforce module assembly boundaries, layer source/type boundaries, API persistence isolation, BuildingBlocks module independence, and SharedKernel framework-light constraints.

Existing EF migration designer snapshots still contain historical entity-name strings from prior namespaces. No schema migration was created for this structural refactor.
