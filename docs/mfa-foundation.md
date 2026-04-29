# MFA Foundation

Date: 2026-04-29

## Current Scope

Tailbook now has a backend-only MFA factor model for future second-factor enforcement. The current implementation intentionally does not alter login behavior and does not expose any frontend UI claiming MFA protection is complete.

Implemented endpoints for the authenticated current user:

- `GET /api/identity/me/mfa/factors`
- `POST /api/identity/me/mfa/email`
- `DELETE /api/identity/me/mfa/factors/{factorId}`

The only factor type currently modeled is `EmailOtp`. Enabling a factor records durable state tied to the user email. Disabling marks the factor disabled rather than deleting the record.

## Not Yet Implemented

- Login-time MFA challenge and verification.
- OTP generation, expiry, delivery, and replay protection.
- Recovery codes.
- Admin/operator reset flows.
- Frontend enrollment UX.

## Next Design Steps

Before enforcement is enabled, add a challenge table with hashed OTP codes, rate limiting, notification delivery, audit events, and a login response contract that explicitly indicates an MFA challenge is required.
