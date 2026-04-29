# Auth Session Handling

Date: 2026-04-29

## Current behavior

- Admin and groomer apps authenticate through `/api/identity/auth/login`.
- Client portal login and registration authenticate through `/api/client/auth/login` and `/api/client/auth/register`.
- All three apps store the short-lived access token and refresh token in `localStorage`.
- API clients retry one authenticated request after a `401` by exchanging the stored refresh token.
- Failed refresh, malformed refresh responses, or missing tokens clear local session state and emit the app-specific unauthorized event.
- Logout clears local state immediately and sends a best-effort revoke request:
  - Admin/groomer: `/api/identity/auth/revoke`
  - Client portal: `/api/client/auth/revoke`

## Remaining security limitations

- Browser tokens are still stored in `localStorage`; this remains sensitive to successful XSS. A production hardening pass should move refresh tokens to secure, HTTP-only, same-site cookies once the API and deployment topology support that contract.
- Logout revoke is best-effort so users are not trapped in a broken UI when the API is offline. Backend refresh token expiry and rotation remain the final control if revoke cannot reach the API.
- Session refresh is intentionally single-retry per request. The apps do not run a background refresh loop.
- MFA is not yet implemented; the session model is still single-factor after password authentication.
