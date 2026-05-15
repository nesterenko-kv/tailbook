# SMTP Credential Rollout

## Overview
This runbook covers configuring and verifying SMTP notification delivery for production password-reset and MFA OTP emails.

## Prerequisites
- Access to the SMTP server credentials (host, port, username, password).
- SMTP server must allow connections from the Tailbook API server IP.
- TLS certificate on the SMTP server (if `SmtpEnableSsl=true`).

## Configuration

### Required settings
| Setting | Environment variable | Example |
| --- | --- | --- |
| Provider | `NOTIFICATIONS_PROVIDER` | `Smtp` |
| SMTP host | `NOTIFICATIONS_SMTP_HOST` | `smtp.sendgrid.net` |
| SMTP port | `NOTIFICATIONS_SMTP_PORT` | `587` |
| TLS | `NOTIFICATIONS_SMTP_ENABLE_SSL` | `true` |
| From email | `NOTIFICATIONS_SMTP_FROM_EMAIL` | `noreply@pilot.example.com` |
| From name | `NOTIFICATIONS_SMTP_FROM_NAME` | `Tailbook` |
| Username | `NOTIFICATIONS_SMTP_USERNAME` | `apikey` |
| Password | `NOTIFICATIONS_SMTP_PASSWORD` | `SG.xxxxx` |

### Optional settings
| Setting | Environment variable | Default |
| --- | --- | --- |
| Timeout (seconds) | `NOTIFICATIONS_SMTP_TIMEOUT_SECONDS` | `30` |

## Rollout steps

### Step 1: Configure SMTP credentials
1. Add the SMTP settings to your `.env` file or production environment:
   ```env
   NOTIFICATIONS_PROVIDER=Smtp
   NOTIFICATIONS_SMTP_HOST=smtp.example.com
   NOTIFICATIONS_SMTP_PORT=587
   NOTIFICATIONS_SMTP_ENABLE_SSL=true
   NOTIFICATIONS_SMTP_FROM_EMAIL=noreply@tailbook.example.com
   NOTIFICATIONS_SMTP_FROM_NAME=Tailbook
   NOTIFICATIONS_SMTP_USERNAME=your-smtp-username
   NOTIFICATIONS_SMTP_PASSWORD=your-smtp-password
   NOTIFICATIONS_SMTP_TIMEOUT_SECONDS=30
   ```

2. Restart the API service:
   ```bash
   docker compose -f docker-compose.production.yml restart api
   ```

### Step 2: Verify SMTP connectivity
1. Check provider health:
   ```bash
   curl https://api.tailbook.test/api/admin/notifications/provider/health
   ```
   Expected: `{"providerType":"Smtp","isConfigured":true,...}`

2. Trigger a test notification (e.g., password reset):
   ```bash
   curl -X POST https://api.tailbook.test/api/identity/auth/request-password-reset \
     -H "Content-Type: application/json" \
     -d '{"email":"test-recipient@example.com"}'
   ```

3. Process the outbox:
   ```bash
   curl -X POST https://api.tailbook.test/api/admin/notifications/outbox/process
   ```

4. Check the notification job status:
   ```bash
   curl "https://api.tailbook.test/api/admin/notifications/jobs?status=Failed"
   ```
   If no failed jobs appear, SMTP delivery is working.

### Step 3: Monitor
1. Check provider health for failure rate:
   ```bash
   curl https://api.tailbook.test/api/admin/notifications/provider/health
   ```
2. Monitor the notification dashboard for dead-letter counts:
   ```bash
   curl https://api.tailbook.test/api/admin/notifications/dashboard
   ```

## Troubleshooting

### SMTP host unreachable
- Verify the SMTP hostname resolves from the API container.
- Check network security groups/firewalls allow outbound SMTP traffic.

### Authentication failed
- Verify `SmtpUsername` and `SmtpPassword` are correct.
- For API-key based providers (SendGrid, Mailgun), check if the API key has sending permissions.

### TLS negotiation failed
- Try `SmtpEnableSsl=false` for STARTTLS on port 587, or `true` for SSL on port 465.
- Verify the SMTP server certificate is valid and trusted by the API container.

### Rate limiting
- Some SMTP providers impose sending limits. Check provider documentation for limits.
- The notification system respects `RetryBaseDelaySeconds` for automated retry backoff.

## Switching back to LocalFile
If SMTP delivery has persistent issues, switch back:
1. Set `NOTIFICATIONS_PROVIDER=LocalFile`.
2. Restart the API.
3. Requeue any dead-lettered jobs after the SMTP issue is resolved.
