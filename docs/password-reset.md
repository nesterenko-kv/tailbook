# Password Reset

Date: 2026-04-29

## Endpoints

- `POST /api/identity/auth/request-password-reset`
  - Body: `{ "email": "user@example.com" }`
  - Always returns `202 Accepted` for valid email syntax so callers cannot distinguish unknown accounts from known accounts.
- `POST /api/identity/auth/reset-password`
  - Body: `{ "token": "<reset-token>", "newPassword": "NewPass123!" }`
  - Returns `204 No Content` on success.
  - Returns `400 Bad Request` for invalid, expired, reused, or weak reset requests.

## Token Handling

- Raw reset tokens are generated with cryptographically secure random bytes.
- Only a SHA-256 token hash is stored in `iam.iam_password_reset_tokens`.
- Tokens expire according to `PasswordReset:ExpirationMinutes`.
- Successful reset marks the token used and revokes active refresh tokens for that user.

## Local Notification Path

The request endpoint emits a `PasswordResetRequested` outbox message. The Notifications module includes a local-file template for development and test environments when no email provider is configured. Treat local notification files as sensitive because reset links/tokens are bearer credentials until expiry.
