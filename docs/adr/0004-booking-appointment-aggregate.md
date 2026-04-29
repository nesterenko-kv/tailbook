# 0004 Booking Appointment Aggregate

Status: Accepted

Date: 2026-04-29

## Context

Booking owns the intent to schedule a pet appointment with a groomer. The same appointment is created from admin and request-conversion flows, rescheduled by Booking, and moved through visit lifecycle states through the VisitOperations integration port.

The module also has existing HTTP API behavior where `DateTime` inputs without an explicit UTC kind, and local-kind inputs, have been treated as UTC wall-clock values.

## Decision

`Appointment` is the aggregate root for appointment scheduling state. It owns the appointment period, lifecycle status, cancellation state, version, and appointment items that were snapshotted at booking time. `AppointmentItem` remains aggregate-owned and is only created through `Appointment`.

`BookingPeriod` is a strict value object. It accepts only non-default UTC values and requires `EndAtUtc > StartAtUtc`. API compatibility does not belong in this value object.

`BookingTimeInputNormalizer` is the application-boundary compatibility point for legacy appointment time inputs. It preserves the current public behavior by treating unspecified and local `DateTime` values as UTC wall-clock values before creating `BookingPeriod`.

Outbox publishing remains in the application layer. The codebase already uses application services to publish integration events, and the current Booking aggregate does not need a new domain event dispatcher. Outbox payloads should be created only after successful aggregate mutation and should use final aggregate state.

## Guardrails

- Do not add public setters to `Appointment` or `AppointmentItem`.
- Do not let application code mutate appointment status, period, cancellation fields, or items directly.
- Do not relax `BookingPeriod` to accept local or unspecified values.
- Do not move legacy API time compatibility into the domain model.
- Do not publish Booking appointment integration events before aggregate methods succeed.
- Keep cross-module coordination through existing building-block abstractions, especially for VisitOperations.
