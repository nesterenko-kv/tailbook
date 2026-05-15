# Notification Provider Outage Runbook

## Overview
This runbook covers operational procedures for notification delivery failures, provider outages, and dead-letter management.

## Monitored conditions

| Condition | Detection | Severity |
|-----------|-----------|----------|
| SMTP host unreachable | `GET /api/admin/notifications/provider/health` shows failures | High |
| High dead-letter volume | Dashboard shows >10 dead-letter jobs | Medium |
| All recent deliveries failing | Provider health shows 100% failure rate in last 24h | High |
| LocalFile disk full | Notification sink throws IOException | High |

## Dashboard
View notification operational status at:
- Dashboard rollups: `GET /api/admin/notifications/dashboard`
- Provider health: `GET /api/admin/notifications/provider/health`
- Job list with filters: `GET /api/admin/notifications/jobs`

## Procedures

### SMTP provider outage

1. **Verify configuration**: Check `Notifications:SmtpHost`, `Notifications:SmtpPort`, and credentials in `.env` or environment.
2. **Test connectivity**: Use `telnet <smtp-host> <smtp-port>` or equivalent from the API container.
3. **Check certificate**: If `SmtpEnableSsl=true`, verify the SMTP server certificate is valid and not expired.
4. **Temporary fallback**: Switch to `LocalFile` provider by setting `NOTIFICATIONS_PROVIDER=LocalFile` and restarting the API. Notifications will be written to the local file until SMTP is restored.
5. **Requeue jobs**: After SMTP is restored, requeue dead-lettered jobs through `POST /api/admin/notifications/jobs/{jobId}/requeue` or use the admin web UI.
6. **Monitor recovery**: Check provider health endpoint for successful deliveries after requeue.

### High dead-letter volume

1. **Identify cause**: Check `GET /api/admin/notifications/dashboard` for dead-letter breakdown by event type.
2. **Inspect jobs**: Filter jobs by status `DeadLetter` and inspect the `LastErrorMessage` field.
3. **Common causes**:
   - Invalid recipient email address → abandon the job, notify operator to fix contact data.
   - Template rendering error → check notification templates in seed data.
   - Protected payload key rotated → ensure `SensitivePayloadProtection:Key` has not changed since the outbox message was created.
4. **Resolve**: Fix the root cause, then requeue affected jobs through the admin UI.

### LocalFile provider issues

1. **Check disk space**: Verify the volume mounted at the `LocalFilePath` location has available space.
2. **Check file permissions**: Ensure the API container user can write to the file path.
3. **Rotate file**: Stop the API, move/archive the notification file, restart the API. Unprocessed outbox messages will be re-dispatched.

## Recovery verification

After resolving an outage:
1. Call `POST /api/admin/notifications/outbox/process` to trigger immediate outbox processing.
2. Check `GET /api/admin/notifications/dashboard` for declining dead-letter counts.
3. Check `GET /api/admin/notifications/provider/health` for successful deliveries.
4. For SMTP, verify a test notification is delivered to the intended recipient.

## Prevention

- Monitor provider health endpoint proactively with external uptime checks.
- Set up alerts on dead-letter count thresholds through the OpenTelemetry metrics pipeline.
- Rotate `SensitivePayloadProtection:Key` only after all pending password-reset outbox messages are delivered or abandoned.
- Keep SMTP credentials and host configuration in environment variables, not in code.
