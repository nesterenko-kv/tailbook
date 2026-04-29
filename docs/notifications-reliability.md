# Notifications Reliability

Date: 2026-04-29

## Outbox Processing

Notification processing reads unprocessed integration outbox messages and maps recognized event types to active notification templates.

Reliability behavior:

- A notification job is created once per source outbox message.
- Failed delivery keeps the source outbox message unprocessed so it can be retried.
- Retry attempts reuse the existing notification job and increment `AttemptCount`.
- Successful retry marks the job `Sent`, clears `LastErrorMessage`, records a sent delivery attempt, and marks the outbox message processed.
- Messages without a matching active template are marked processed because there is no notification work to retry.

## Admin Visibility

`GET /api/admin/notifications/jobs` now returns status, attempt count, sent timestamp, and the latest failure message. Use `?status=Failed` to inspect currently retryable failures.

## Remaining Work

- Add max-attempt/backoff policy.
- Add dead-letter status for permanently failing jobs.
- Add dashboard filtering by event type and date range.
