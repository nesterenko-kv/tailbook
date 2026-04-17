# ADR 0003 - Operations and recovery baseline

## Status
Accepted

## Decision
Tailbook keeps operational recovery inside the repo via:
- PostgreSQL backup/restore scripts
- runbooks for local production and restore
- a documented seed/import strategy
- a final repo map and ADR index for handoff

## Why
Operational readiness is part of the product delivery baseline, not a separate afterthought.
