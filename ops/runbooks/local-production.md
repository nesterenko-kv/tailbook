# Local production-like runbook

## Purpose
Run Tailbook locally in a production-like topology with containerized API, web apps, PostgreSQL, persisted data, health checks, and background outbox processing.

## Start
```bash
cp .env.example .env
docker compose -f docker-compose.production.yml up --build -d
```

## Verify
```bash
docker compose -f docker-compose.production.yml ps
curl http://localhost:5001/health/live
curl http://localhost:5001/health/ready
```

## Expected services
- `postgres`
- `api`
- `admin-web`
- `client-web`
- `groomer-web`

## Notes
- `HttpTransport:EnforceHttpsRedirection` is disabled in local production-like compose because the stack is HTTP-only by default.
- Real production should terminate TLS at a reverse proxy and enable HTTPS redirection + HSTS.
- Browser-facing app origin values must match `AppCors:AllowedOrigins` on the API.
