# Local production-like runbook

## Purpose
Run Tailbook locally in a production-like topology with containerized API, web apps, PostgreSQL, Redis, a Caddy reverse proxy with TLS, persisted data, health checks, and background integration outbox processing.

## Prerequisites
- Docker Compose
- The following entries in your `hosts` file (`C:\Windows\System32\drivers\etc\hosts` or `/etc/hosts`):

```
127.0.0.1 api.tailbook.test admin.tailbook.test client.tailbook.test groomer.tailbook.test
```

## Start
```bash
cp .env.example .env
docker compose -f docker-compose.production.yml up --build -d
```

## Verify
```bash
docker compose -f docker-compose.production.yml config
docker compose -f docker-compose.production.yml ps

# Health checks through Caddy (HTTPS, self-signed cert):
curl -k https://api.tailbook.test/health/live
curl -k https://api.tailbook.test/health/ready

# Web apps:
curl -k https://admin.tailbook.test
curl -k https://client.tailbook.test
curl -k https://groomer.tailbook.test
```

## Expected services
| Service | Internal URL | External URL |
| --- | --- | --- |
| `caddy` | — | `https://*.tailbook.test:443` |
| `api` | `http://api:8080` | `https://api.tailbook.test` |
| `admin-web` | `http://admin-web:3000` | `https://admin.tailbook.test` |
| `client-web` | `http://client-web:3000` | `https://client.tailbook.test` |
| `groomer-web` | `http://groomer-web:3000` | `https://groomer.tailbook.test` |
| `postgres` | `postgres:5432` | internal only |
| `redis` | `redis:6379` | internal only |

## TLS modes

### Local development (default)
- Uses `TLS_MODE=internal` → Caddy generates self-signed certs.
- Browser shows a security warning; click through or add the CA cert.
- No real DNS or public domain needed.

### Production
- Set `TLS_DOMAIN` to your real domain (e.g., `pilot.example.com`).
- Set `TLS_MODE=tls` and `CADDY_EMAIL` to your email.
- Caddy automatically provisions Let's Encrypt certificates for `api.*`, `admin.*`, `client.*`, `groomer.*`.
- Ensure DNS A/AAAA records point each subdomain to your server IP.

## Configuration
Before exposing the stack beyond a developer machine, replace the placeholder security and delivery values in `.env`:

- `Jwt__SigningKey`
- `SensitivePayloadProtection__Key`
- `REDIS_PASSWORD`
- `BOOTSTRAP_ADMIN_PASSWORD`
- `PASSWORD_RESET_URL_BASE`
- `NOTIFICATIONS_PROVIDER`
- `NOTIFICATIONS_SMTP_HOST`
- `NOTIFICATIONS_SMTP_FROM_EMAIL`
- `NOTIFICATIONS_SMTP_USERNAME`
- `NOTIFICATIONS_SMTP_PASSWORD`

Use `NOTIFICATIONS_PROVIDER=LocalFile` only for local development. Local notification files can contain password-reset links in dispatch envelopes and must be treated as sensitive. Use `NOTIFICATIONS_PROVIDER=Smtp` with real SMTP settings for production-like reset-link delivery.

## Notes
- `HttpTransport:EnforceHttpsRedirection` and `HttpTransport:UseHsts` are enabled when behind Caddy. Caddy handles external TLS; the API trusts `X-Forwarded-Proto` from Caddy.
- Browser-facing app origin values must match `AppCors:AllowedOrigins` on the API and the Caddy routes.
- Replace every `change-me` and bootstrap value in `.env` before exposing the stack to a network.
- Rotate `SensitivePayloadProtection__Key` only after pending password-reset notifications are processed or intentionally abandoned; pending protected reset links require the key that created them.
- `API_ALLOWED_HOSTS` must include the public API host (auto-configured from `TLS_DOMAIN`).
- The `caddy` service stores certificate data in the `caddy-data` Docker volume.
- For local testing with `-k`/`--insecure` curl flags, the self-signed cert warning is expected.
