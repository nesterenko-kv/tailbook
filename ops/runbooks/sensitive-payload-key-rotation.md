# Sensitive Payload Protection Key Rotation

## Overview
This runbook covers rotating the `SensitivePayloadProtection:Key` used to protect password-reset links and MFA OTP codes in outbox payloads.

## How it works

The `AesGcmSensitivePayloadProtector` uses AES-GCM with a key derived from `SensitivePayloadProtection:Key`. During rotation, `SensitivePayloadProtection:PreviousKey` can be configured so that messages protected with the old key can still be unprotected while new messages use the new key.

The protector tries keys in order:
1. Current `Key` — used for both protect and unprotect.
2. `PreviousKey` — used for unprotect only, fallback if the current key fails.

## Rotation procedure

### Before you begin
- Ensure all operators are aware of the maintenance window.
- Key rotation does not require downtime or service restart if environment variables are updated via rolling update.

### Step 1: Verify current state
1. Check that no critical pending outbox messages exist (or document that they will need the previous key):
   ```bash
   curl https://api.tailbook.test/api/admin/notifications/dashboard
   ```
2. Record the current dead-letter and pending message counts.

### Step 2: Set the new key and previous key
1. Update the environment or `.env` file:
   ```env
   SensitivePayloadProtection__Key=<new-key-at-least-32-chars>
   SensitivePayloadProtection__PreviousKey=<old-key-at-least-32-chars>
   ```
2. Restart the API service:
   ```bash
   docker compose -f docker-compose.production.yml restart api
   ```

### Step 3: Verify new protection
1. Trigger a test password-reset request:
   ```bash
   curl -X POST https://api.tailbook.test/api/identity/auth/request-password-reset \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com"}'
   ```
2. Process the outbox:
   ```bash
   curl -X POST https://api.tailbook.test/api/admin/notifications/outbox/process
   ```
3. Verify the notification job shows a successful delivery (or pending for SMTP):
   ```bash
   curl https://api.tailbook.test/api/admin/notifications/jobs?status=Sent
   ```

### Step 4: Monitor old-key message delivery
1. Check that pending outbox messages from before the rotation are still being processed:
   - Old protected payloads will use `PreviousKey` for unprotection.
2. Monitor the dashboard for dead-letter counts:
   ```bash
   curl https://api.tailbook.test/api/admin/notifications/dashboard
   ```

### Step 5: Remove PreviousKey after transition
1. Wait for all outbox messages created before the rotation to be processed (delivered, abandoned, or expired).
2. Remove `SensitivePayloadProtection__PreviousKey` from the configuration.
3. Restart the API:
   ```bash
   docker compose -f docker-compose.production.yml restart api
   ```

## Verification
After rotation:
- `GET /api/admin/notifications/provider/health` shows successful deliveries.
- Password-reset notification jobs are processed without `LastErrorMessage` about unprotection failures.
- Dead-letter count does not increase for "protected payload could not be unprotected" errors.

## Rollback
To revert to the old key:
1. Set `Key` back to the original value.
2. Remove `PreviousKey` or set it to the rotated key if messages were created with it.
3. Restart the API.

## Troubleshooting

### "Protected payload could not be unprotected with any configured key"
Cause: Neither `Key` nor `PreviousKey` can decrypt the payload.
Solution: Ensure both keys are correctly set and match the keys used when the payload was created. Check for typos, trailing whitespace, or encoding issues.

### Dead-letter spike after rotation
Cause: Outbox messages from before rotation have `ProtectedResetLink` values that the new `PreviousKey` cannot unprotect.
Solution: Verify `PreviousKey` matches the exact value of the previous `Key`. Restart the API after correcting.
