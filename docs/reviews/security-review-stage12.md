# Stage 12 security review summary

## Applied hardening
- JWT settings validated at startup.
- Bootstrap admin settings validated at startup.
- Staff scheduling timezone validated at startup.
- Notification worker settings validated at startup.
- API security headers added (`nosniff`, `DENY`, `no-referrer`, trace id header).
- Request logging avoids logging auth headers/body payloads.
- Swagger remains development-only.
- Login endpoints throttle repeated failed attempts by normalized email and return `429` with `Retry-After` while locked out.
- Separate admin/client/groomer API surfaces remain preserved.

## Remaining risks
- Local production-like compose is HTTP-only unless a reverse proxy/TLS terminator is added.
- Refresh tokens, password reset, and MFA are not implemented.
- Fine-grained per-entity permission scopes are still basic.
- Secrets are still environment-driven; a real secret store is outside this repo.
