# Observability runbook

## Request correlation
- API responses include `X-Trace-Id`.
- API request logs include method, path, status code, elapsed milliseconds, and the same trace ID.
- Request bodies, authorization headers, refresh tokens, password reset tokens, and cookies are not logged.

## Health endpoints
```bash
curl http://localhost:5001/health/live
curl http://localhost:5001/health/ready
```

- `/health/live` confirms the process can answer HTTP.
- `/health/ready` returns JSON with overall status, total duration, and per-check status.
- Check errors expose the exception type only, not raw exception messages or connection strings.

## Startup diagnostics
Startup logs include sanitized runtime shape:
- environment name
- database host and database name
- CORS origin count
- HTTPS redirection and HSTS flags
- notification background-processing state and poll interval
- staff scheduling time zone

Secrets and full connection strings must remain in environment variables or secret stores, not logs.
