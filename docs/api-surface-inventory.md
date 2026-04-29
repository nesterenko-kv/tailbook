# API Surface Inventory

Date: 2026-04-29

This inventory maps Tailbook backend API surfaces by module and consumer. It is based on FastEndpoints route declarations and the current Next.js API call sites.

## Consumer Surfaces

| Consumer | Route prefix | Auth posture | Primary callers |
| --- | --- | --- | --- |
| Admin web | `/api/admin/*`, `/api/identity/*` | JWT with admin/manager permissions | `apps/admin-web` |
| Client web | `/api/client/*`, `/api/public/*` | Client JWT for portal pages; anonymous public booking | `apps/client-web` |
| Groomer web | `/api/groomer/*`, `/api/identity/*` | JWT with groomer permissions | `apps/groomer-web` |
| Public booking | `/api/public/*` | Anonymous, optionally resolves authenticated client actor | unauthenticated booking pages in `apps/client-web` |
| Operations | `/health/*`, `/` | anonymous health/root endpoints | infrastructure and manual checks |

## Backend Endpoints By Module

### Identity

Admin:

- `GET /api/admin/iam/users`
- `POST /api/admin/iam/users`
- `GET /api/admin/iam/users/{id}`
- `POST /api/admin/iam/users/{id}/roles`
- `GET /api/admin/iam/roles`
- `GET /api/admin/iam/permissions`

Auth:

- `POST /api/identity/auth/login`
- `POST /api/identity/auth/refresh`
- `POST /api/identity/auth/revoke`
- `GET /api/identity/me`

Client auth:

- `POST /api/client/auth/register`
- `POST /api/client/auth/login`
- `POST /api/client/auth/refresh`
- `POST /api/client/auth/revoke`
- `GET /api/client/me`

Known gaps:

- No password reset endpoints.
- No MFA endpoints or second-factor challenge flow.
- Admin/groomer web login currently share `/api/identity/auth/login`; client web uses `/api/client/auth/login`.

### Customer

Admin:

- `GET /api/admin/clients`
- `POST /api/admin/clients`
- `GET /api/admin/clients/{id}`
- `POST /api/admin/clients/{clientId}/contacts`
- `POST /api/admin/contacts/{contactId}/methods`
- `POST /api/admin/pets/{petId}/contacts/{contactId}`
- `GET /api/admin/pets/{petId}/contacts`

Client:

- `GET /api/client/me/contact-preferences`
- `PATCH /api/client/me/contact-preferences`

Known gaps:

- Contact-method mutation coverage is creation-focused; richer update/deactivate flows are not present.

### Pets

Admin:

- `GET /api/admin/pets/catalog`
- `POST /api/admin/pets`
- `GET /api/admin/pets/{id}`
- `PATCH /api/admin/pets/{id}`

Client:

- `GET /api/client/me/pets`
- `GET /api/client/me/pets/{petId}`

Public:

- `GET /api/public/pets/catalog`

Known gaps:

- No `GET /api/admin/pets` list endpoint. `apps/admin-web/app/(protected)/pets/page.tsx` works around this with direct ID lookup plus locally stored recent IDs.

### Catalog

Admin:

- `GET /api/admin/catalog/offers`
- `POST /api/admin/catalog/offers`
- `GET /api/admin/catalog/offers/{id}`
- `POST /api/admin/catalog/offers/{offerId}/versions`
- `POST /api/admin/catalog/offer-versions/{versionId}/components`
- `POST /api/admin/catalog/offer-versions/{versionId}/publish`
- `GET /api/admin/catalog/procedures`
- `POST /api/admin/catalog/procedures`
- `GET /api/admin/pricing/rule-sets`
- `POST /api/admin/pricing/rule-sets`
- `POST /api/admin/pricing/rule-sets/{ruleSetId}/rules`
- `POST /api/admin/pricing/rule-sets/{ruleSetId}/publish`
- `GET /api/admin/duration/rule-sets`
- `POST /api/admin/duration/rule-sets`
- `POST /api/admin/duration/rule-sets/{ruleSetId}/rules`
- `POST /api/admin/duration/rule-sets/{ruleSetId}/publish`

Known gaps:

- List endpoints for offers/procedures/rule sets are unpaged arrays or item envelopes, unlike newer paged admin list surfaces.

### Staff

Admin:

- `GET /api/admin/groomers`
- `POST /api/admin/groomers`
- `GET /api/admin/groomers/{groomerId}`
- `POST /api/admin/groomers/{groomerId}/capabilities`
- `POST /api/admin/groomers/{groomerId}/working-schedules`
- `POST /api/admin/groomers/{groomerId}/time-blocks`
- `GET /api/admin/groomers/{groomerId}/schedule`
- `POST /api/admin/groomers/{groomerId}/availability/check`

Known gaps:

- Operational staff list exists. Future hardening should focus on update/deactivate flows and schedule validation.

### Booking

Admin:

- `GET /api/admin/booking-requests`
- `POST /api/admin/booking-requests`
- `GET /api/admin/booking-requests/{bookingRequestId}`
- `POST /api/admin/booking-requests/{bookingRequestId}/attach-context`
- `POST /api/admin/booking-requests/{bookingRequestId}/convert`
- `GET /api/admin/appointments`
- `POST /api/admin/appointments`
- `GET /api/admin/appointments/{appointmentId}`
- `POST /api/admin/appointments/{appointmentId}/reschedule`
- `POST /api/admin/appointments/{appointmentId}/cancel`
- `POST /api/admin/quotes/preview`

Client:

- `GET /api/client/appointments`
- `GET /api/client/appointments/{appointmentId}`
- `GET /api/client/booking-offers`
- `POST /api/client/quotes/preview`
- `POST /api/client/booking-requests`

Groomer:

- `GET /api/groomer/me/appointments`
- `GET /api/groomer/appointments/{appointmentId}`

Public:

- `POST /api/public/booking-offers`
- `POST /api/public/quotes/preview`
- `POST /api/public/booking-planner`
- `POST /api/public/booking-requests`

Known gaps:

- Admin appointment list exists and is paged.
- Public booking uses POST for read-like planner and offer resolution because request bodies carry pet selection state.

### Visit Operations

Admin:

- `POST /api/admin/appointments/{appointmentId}/check-in`
- `GET /api/admin/visits/{visitId}`
- `POST /api/admin/visits/{visitId}/performed-procedures`
- `POST /api/admin/visits/{visitId}/skipped-components`
- `POST /api/admin/visits/{visitId}/adjustments`
- `POST /api/admin/visits/{visitId}/complete`
- `POST /api/admin/visits/{visitId}/close`

Groomer:

- `POST /api/groomer/appointments/{appointmentId}/check-in`
- `GET /api/groomer/appointments/{appointmentId}/visit`
- `GET /api/groomer/visits/{visitId}`
- `POST /api/groomer/visits/{visitId}/performed-procedures`
- `POST /api/groomer/visits/{visitId}/skipped-components`

Known gaps:

- No `GET /api/admin/visits` list endpoint. `apps/admin-web/app/(protected)/visits/page.tsx` works around this with direct ID lookup plus locally stored recent IDs.
- Groomer can find visits through assigned appointment context, but there is no direct groomer visit list.

### Audit

Admin:

- `GET /api/admin/audit`
- `GET /api/admin/audit/access`

Known gaps:

- Audit coverage depends on module call sites. Sensitive identity, reset, and visit-financial actions need coverage review.

### Notifications

Admin:

- `GET /api/admin/notifications/jobs`
- `POST /api/admin/notifications/outbox/process`

Known gaps:

- Listing exists, but retry/failure observability needs a reliability pass.

### Reporting

Admin:

- `GET /api/admin/reports/estimate-accuracy`
- `GET /api/admin/reports/package-performance`

Known gaps:

- Reporting correctness and query performance need scenario review before expanding.

## Frontend Workaround Flows

- Admin pets page depends on `/api/admin/pets/catalog`, `/api/admin/clients`, `POST /api/admin/pets`, direct ID lookup, and local recent IDs because no admin pet list endpoint exists.
- Admin visits page depends on manually pasted visit IDs and local recent IDs because no admin visit list endpoint exists.
- Admin appointments, booking requests, clients, IAM users, groomers, audit, and pricing pages already consume list or list-like endpoints.
- Client booking pages use local booking context across steps and anonymous public endpoints. This needs resilience for refresh, missing context, invalid IDs, API failures, and duplicate submit.
- Admin/client/groomer apps have separate API clients with similar error and unauthorized handling, which is a candidate for careful consolidation.

## Priority Contract Gaps

1. Add `GET /api/admin/pets` with pagination and filters (`search`, `clientId`, taxonomy filters).
2. Add `GET /api/admin/visits` with pagination and filters (`status`, date range, `groomerId`, `appointmentId`).
3. Add password reset request and reset endpoints without user enumeration.
4. Add MFA-ready identity model and safe backend foundation.
5. Review permission granularity for identity role assignment, visit financial adjustment, notification processing, and reporting access.
