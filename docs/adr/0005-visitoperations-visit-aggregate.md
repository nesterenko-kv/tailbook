# 0005 VisitOperations Visit Aggregate

Status: Accepted

Date: 2026-04-29

## Context

VisitOperations owns execution of a checked-in appointment: the visit lifecycle, execution item snapshots, performed procedures, skipped included components, and final price adjustments. The module reads appointment, pet, groomer, and catalog data through existing building-block abstractions and should not own those cross-module records.

Before this decision, `Visit` and its child rows were mostly public-setter data bags, with lifecycle and child invariants enforced in application services.

## Decision

`Visit` is the aggregate root. It owns:

- `VisitExecutionItem` snapshots copied from the appointment at check-in;
- `VisitPerformedProcedure` records under an execution item;
- `VisitSkippedComponent` records under an execution item;
- `VisitPriceAdjustment` rows that affect the visit final total.

No new value object was introduced. The current model already has stable scalar columns, and the useful invariants are lifecycle, ownership, uniqueness, and final-total rules rather than a reusable value concept.

Application services still perform cross-module checks, such as whether a catalog component belongs to the selected offer version and whether default expected components are accounted for before completion. The aggregate enforces local invariants after those references are resolved.

## Invariants

- A visit must reference an appointment and have at least one execution item.
- Execution items require valid appointment item, offer, offer version, display snapshot, positive quantity, non-negative price, and positive duration snapshots.
- Performed procedures and skipped components must belong to a visit execution item and cannot be duplicated for the same item.
- Price adjustments require sign `-1` or `1`, a positive amount, and a reason code.
- A price adjustment cannot make the final visit total negative.
- Closed visits cannot be edited.

## Lifecycle

`Visit` starts in `Open` when an appointment is checked in. Recording a performed procedure or skipped component moves it to `InProgress`. A visit can be completed from `Open` or `InProgress`, which moves it to `AwaitingFinalization`. It can be closed only from `AwaitingFinalization`.

## Persistence And Events

EF Core maps aggregate-owned collections through backing fields while keeping existing tables and columns. No migration is required.

Outbox publishing remains in the application layer. Existing application services already coordinate cross-module appointment state changes and integration events. Events are published only after aggregate methods succeed, and payloads use final aggregate state.

## Guardrails

- Do not add public setters to `Visit` or its child entities.
- Do not let application code directly set visit lifecycle fields or create child rows without using aggregate methods.
- Do not make `Visit` own Booking, Pets, Staff, or Catalog data; keep those as IDs and read models.
- Do not introduce domain-event infrastructure unless the wider codebase adopts it.
- Keep default-component completion checks at the application boundary unless Catalog data becomes part of a stable Visit snapshot.
