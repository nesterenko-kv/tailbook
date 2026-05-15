# Release Regression Gate

## Purpose

A repeatable pre-release validation to confirm the monorepo is healthy, all tests pass, all apps build, and no security regressions are present. Run this before every production-pilot release.

## Automated gate

```bash
pwsh ./ops/scripts/release-regression.ps1
```

The script exits non-zero on any failure and prints a pass/fail summary.

## Manual checklist

Use this when you cannot run the script (CI-less deployment, partial environment) or as a cross-check:

### 1. Backend
- [ ] `dotnet restore backend/Tailbook.slnx`
- [ ] `dotnet build backend/Tailbook.slnx --no-restore` — 0 warnings, 0 errors
- [ ] `dotnet test backend/Tailbook.slnx --no-build` — all tests pass (Architecture + API)

### 2. Frontend
- [ ] `pnpm lint` — all three apps pass
- [ ] `pnpm typecheck` — all three apps pass
- [ ] `pnpm build` — all three apps pass

### 3. Docker / infrastructure
- [ ] `docker compose config` — valid
- [ ] `docker compose -f docker-compose.production.yml config` — valid
- [ ] `git diff --check` — no whitespace errors

### 4. Security
- [ ] No `.env` files staged or committed
- [ ] No uncommitted secrets or credentials in tracked files
- [ ] Swagger remains disabled (check `appsettings.Production.json`)
- [ ] CORS origins match production domains
- [ ] HSTS and HTTPS redirection enabled in production compose

### 5. Contract sync
- [ ] New API endpoints documented in `docs/api-surface-inventory.md`
- [ ] Changed contracts reflected in frontend types and call sites
- [ ] `docs/backlog.md` and `ITERATION_LOG.md` updated

### 6. Production configuration
- [ ] `Jwt__SigningKey` is a strong unique value
- [ ] `SensitivePayloadProtection__Key` is a strong unique value
- [ ] `BOOTSTRAP_ADMIN_PASSWORD` is a strong unique value
- [ ] `BrowserSessions__TokenTransport` is `RefreshCookie`
- [ ] `NOTIFICATIONS_PROVIDER` is `Smtp` (not `LocalFile`)
- [ ] SMTP credentials are configured for production email
- [ ] `TLS_DOMAIN` points to the production host
- [ ] DNS A/AAAA records exist for all subdomains

## Release cut

After the gate passes:

```bash
git tag pilot-v<date>-<build>
git push origin pilot-v<date>-<build>
```

Then deploy following `ops/runbooks/local-production.md` (substituting production TLS domain and SMTP settings).

## Rollback

If a post-release issue is found:

1. Revert the release commit on `main`.
2. Push the revert.
3. Deploy the previous known-good tag.

See `docs/auth-session-handling.md` and individual module runbooks for component-specific rollback paths.
