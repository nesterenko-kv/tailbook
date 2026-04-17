# ADR 0002 - Local production runtime hardening

## Status
Accepted

## Decision
Tailbook gains a production-like local deployment shape based on:
- multi-stage Dockerfiles for API and each Next.js app
- `docker-compose.production.yml` as the local production topology
- startup option validation for critical configuration
- live/readiness health endpoints
- request logging and API security headers
- optional background outbox processing

## Why
This keeps the repo self-hostable and easier to operate locally before any real production hosting decisions are made.
