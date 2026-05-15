# Identity Stress Tests (k6)

Stress/load tests for the Tailbook identity module using [k6](https://k6.io).

## Quick start

```bash
# 1. Start the API (from repo root)
dotnet run --project backend/src/Tailbook.Api.Host --urls http://localhost:5000

# 2. Run a stress test (in another terminal)
k6 run ops/scripts/stress/identity-login-stress.js

# 3. Find results inline — k6 prints thresholds pass/fail and summary
```

Each script is **self-contained**: it logs in as admin, creates its own test users via the admin API, then runs the workload.

## Scripts

| Script | What it tests | Default load |
|---|---|---|
| `identity-login-stress.js` | Concurrent login + `/me` for created users | ramp to 50 VUs, 30s |
| `identity-authenticated-stress.js` | Admin IAM endpoints + user `/me` mixed | ramp to 30 VUs, 20s |
| `identity-mixed-workload.js` | Full session (login → me → refresh) + admin ops | ramp to 80 VUs, 40s |
| `identity-throttling-stress.js` | Throttling behavior under concurrent bad-login | ramp to 30 VUs, 20s |
| `seed-identity-stress-data.js` | Pre-seeds N test users (optional — scripts self-seed in `setup()`) | 1 VU |

## Customizing

All scripts accept environment variables:

| Variable | Default | Description |
|---|---|---|
| `TAILBOOK_BASE_URL` | `https://localhost:5001` | Base URL of the API |
| `ADMIN_EMAIL` | `admin@tailbook.local` | Bootstrap admin email |
| `ADMIN_PASSWORD` | `MyV3ryC00lAdminP@ss` | Bootstrap admin password |
| `USERS_PER_VU` | `2-3` (varies) | Test users created per VU |

Request the `--insecure-skip-tls-verify` flag for dev self-signed certs:

```bash
k6 run --insecure-skip-tls-verify \
  -e TAILBOOK_BASE_URL=https://localhost:5001 \
  ops/scripts/stress/identity-login-stress.js
```

## Thresholds

Each script defines [k6 thresholds](https://k6.io/docs/using-k6/thresholds/) in the `options` block:

| Script | p(95) latency | Error rate |
|---|---|---|
| login-stress | < 3000ms | < 2% |
| authenticated-stress | < 2000ms | < 1% |
| mixed-workload | < 5000ms | < 5% |
| throttling-stress | < 2000ms | N/A |

Adjust thresholds in the script's `export const options` if baseline differs.

## Notes

- Scripts use `setup()` to create test users on the fly via admin API — no pre-seeding needed.
- Login attempts target `/api/identity/auth/login` (regular identity login, not client portal).
- Throttling test targets a single non-existent email to trigger rate-limiting.
- Admin endpoints tested: `GET /api/admin/iam/users`, `/roles`, `/permissions`.
