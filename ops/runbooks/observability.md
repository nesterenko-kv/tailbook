# Observability runbook

## Request correlation

- API responses include `X-Trace-Id`.
- `X-Trace-Id` uses the active OpenTelemetry activity trace ID when one exists, then falls back to the ASP.NET Core trace identifier.
- API request logs include method, path, status code, elapsed milliseconds, trace ID, and span ID.
- Serilog request-scope properties include `TraceId` and `SpanId` for logs emitted while handling the request.
- Unhandled API exceptions return sanitized `application/problem+json` responses with `traceId`.
- Request bodies, authorization headers, refresh tokens, password reset tokens, and cookies are not logged.

### X-Trace-Id triage workflow

1. **Capture the trace ID** from the API response header or problem+json body. Every response includes `X-Trace-Id`.
2. **Search logs** for the trace ID. Structured logs carry `TraceId` and `SpanId` in every entry. Filter by `TraceId` in your log aggregator to reconstruct the full request timeline.
3. **If the trace is missing** — check that the request reached the API (network/firewall), and verify the `X-Trace-Id` header is not stripped by a reverse proxy or CDN (Caddy passes it through by default).
4. **Span-level drill-down** — if OTLP export is configured, use the trace ID in Jaeger/ Aspire Dashboard / your APM to view individual spans: HTTP request → database call → outbox stage → notification delivery.
5. **Correlate cross-service flows** — the `X-Trace-Id` is set at the API host boundary. Downstream calls (outbox, notification delivery, job queue operations) use the same trace ID via OpenTelemetry activity propagation.
6. **Check the `tailbook.request_id` tag** on OpenTelemetry trace activities — it mirrors the `X-Trace-Id` value and is indexed in any OTLP-compatible trace store.

## Health endpoints

```bash
curl http://localhost:5001/health/live
curl http://localhost:5001/health/ready
```

- `/health/live` confirms the process can answer HTTP (runs zero checks).
- `/health/ready` returns JSON with overall status, total duration, and per-check status.
- Check errors expose the exception type only, not raw exception messages or connection strings.
- The host publishes health report telemetry every 30 seconds after startup delay, so health state is visible even when no external monitor is polling the endpoint.

### Health check alerts

| Alert | Condition | Severity | Action |
|---|---|---|---|
| Readiness degraded | `tailbook.health.reports` with `status=Unhealthy` increments above 0 | Pager (critical) | Investigate database connectivity; check Postgres service and network |
| Readiness flapping | `tailbook.health.report.duration` p50 > 500ms or p99 > 2s for 5+ minutes | Warning | Database query performance or connection pool exhaustion |
| Health check leak | `tailbook.health.checks` with `status=Unhealthy` per check grows monotonically without recovery | Pager (critical) | Component outage; check the specific component's error type |
| Liveness failure | Docker/K8s restart count increments | Pager (critical) | Process-level crash; check OOM, unhandled exceptions, or startup timeout |

## OpenTelemetry

The API host records ASP.NET Core request telemetry and .NET runtime metrics through OpenTelemetry.
The API host emits the `Tailbook.Api` meter for host-level diagnostics such as sanitized unhandled exception responses.
The API host emits `Tailbook.Jobs` traces and metrics for FastEndpoints job queue storage operations.
The shared outbox publisher emits `Tailbook.Outbox` traces and metrics when modules stage integration events.
PostgreSQL tracing and Npgsql pool metrics are also enabled so request traces can show database calls and dashboards can track pool usage.
The notifications module emits `Tailbook.Notifications` traces and metrics for outbox processing cycles, message outcomes, delivery attempts, and background processor failures.
The audit module emits `Tailbook.Audit` traces and metrics for batch write buffering of access-audit entries.
When an OTLP endpoint is configured, structured `ILogger` records can also be exported as OpenTelemetry logs.

OTLP export is opt-in. Configure a collector endpoint before expecting telemetry to leave the process:

```json
{
  "Telemetry": {
    "Enabled": true,
    "ServiceName": "tailbook-api",
    "DatabasePoolName": "tailbook-main",
    "ExportLogs": true,
    "OtlpEndpoint": "http://otel-collector:4317"
  }
}
```

Leave `Telemetry:OtlpEndpoint` empty for local development without a collector. Do not route telemetry directly to public endpoints; send it through an internal collector or private observability gateway.
Keep `Telemetry:DatabasePoolName` stable and non-secret; it is used as the Npgsql pool name in database metrics.
Set `Telemetry:ExportLogs` to `false` if the collector should receive traces and metrics but not application logs. Serilog console logging remains configured separately.

### Dashboard guidance

Recommended dashboard panels for each meter, with triage notes:

#### API HTTP (`Tailbook.Api`)
| Panel | Metric | Threshold | Triage |
|---|---|---|---|
| Request rate | `tailbook.api.http.server.requests` rate | Pager if drops >90% from baseline for 5 min | Check reverse proxy, DNS, network; may indicate upstream outage |
| Latency (p50/p95/p99) | `tailbook.api.http.server.duration` histogram | P95 > 2s or P99 > 5s sustained 5+ min | Check for slow database queries, N+1 patterns, or resource contention |
| Error rate | `tailbook.api.http.server.requests` × status dimension | 5xx &ne; 0 sustained 3+ min | Check health endpoints, database, and recent deployments |
| Unhandled exceptions | `tailbook.api.unhandled_exceptions` &gt; 0 | Immediate investigation | Application bug; check sanitized problem+json response for `traceId`, search logs by that trace ID |

#### Database (Npgsql built-in)
| Panel | Metric | Threshold | Triage |
|---|---|---|---|
| Connection pool active | `npgsql.connection_pool.active` | &gt; 80% of max pool size sustained 5+ min | Connection leak or traffic spike; check `pooled` connections in health/ready |
| Connection pool idle | `npgsql.connection_pool.idle` | Consistently 0 while pool is active | Pool under-sized; increase `MaxPoolSize` in connection string |
| Wait count | `npgsql.connection_pool.waiting` | &gt; 0 sustained | All connections in use; add connection pooling or reduce concurrent requests |

#### Job queue (`Tailbook.Jobs`)
| Panel | Metric | Threshold | Triage |
|---|---|---|---|
| Operation rate | `tailbook.jobs.storage.operations` rate | Drastic drop from baseline | Job queue processing may be stalled; check `Jobs` table and FastEndpoints job queue health |
| Operation duration | `tailbook.jobs.storage.operation.duration` histogram | P95 > 1s sustained 5+ min | Database contention or slow I/O on `Jobs` table |
| Error operations | `tailbook.jobs.storage.operations` × `result=error` | Any non-zero | Check storage errors — may indicate schema mismatch or deadlock |
| Stale job count | SQL: `SELECT COUNT(*) FROM public."Jobs" WHERE IsComplete = false AND ExpireOn < NOW()` | &gt; 50 | Job queue backlog; increase processor capacity or inspect expired jobs |

#### Notifications (`Tailbook.Notifications`)
| Panel | Metric | Threshold | Triage |
|---|---|---|---|
| Outbox cycle duration | `tailbook.notifications.outbox.duration` histogram | P95 > 30s | Outbox processing bottleneck; check database and provider health |
| Delivery failures | `tailbook.delivery.attempts` × `status=failed` | Rate &gt; 0 sustained 5+ min | Notification provider (SMTP, SMS) may be down; check provider health endpoint. See [notification provider outage runbook](../notification-provider-outage.md) for SMTP outage, high dead-letter, and LocalFile procedures. |
| Background failures | `tailbook.notifications.background.failures` | Any non-zero | Unhandled exception in notification processor; search logs for `Tailbook.Notifications` activity source |

#### Audit (`Tailbook.Audit`)
| Panel | Metric | Threshold | Triage |
|---|---|---|---|
| Batch write failures | `tailbook.audit.batch.writes` × `result=failed` | &gt; 0 | Audit entries lost; check database write capacity and queue capacity |
| Queue depth | `tailbook.audit.queue.enqueued` − `tailbook.audit.queue.dequeued` | &gt; 1000 sustained | Batch writer cannot keep up; increase `Audit:BatchSize` or `Audit:FlushIntervalMilliseconds` |
| Retry rate | `tailbook.audit.batch.retries` | Rate &gt; 0 sustained 5+ min | Transient database errors; check Postgres health |

#### Outbox (`Tailbook.Outbox`)
| Panel | Metric | Threshold | Triage |
|---|---|---|---|
| Message staging rate | `tailbook.outbox.messages.staged` rate | Sharp increase | May indicate integration event storm; check for loops in event handlers |
| Payload size | `tailbook.outbox.payload.size` histogram | P99 > 10KB | Large payloads may cause outbox storage issues; review event schema |

### Configured alert definitions

All alerts assume a 1-minute evaluation window unless otherwise stated. Adjust thresholds based on observed baseline in your environment.

```yaml
# Prometheus-style alert rules — adapt to your alert manager
groups:
  - name: tailbook-api
    rules:
      - alert: ApiHighErrorRate
        expr: rate(tailbook_api_http_server_requests_total{status=~"5.."}[5m]) > 0
        for: 3m
        labels: { severity: critical }
        annotations:
          summary: "API 5xx error rate is non-zero for 3+ minutes"

      - alert: ApiLatencyHigh
        expr: histogram_quantile(0.95, rate(tailbook_api_http_server_duration_seconds_bucket[5m])) > 5
        for: 5m
        labels: { severity: warning }
        annotations:
          summary: "API P95 latency exceeds 5s for 5+ minutes"

      - alert: DatabasePoolExhaustion
        expr: npgsql_connection_pool_waiting > 0
        for: 2m
        labels: { severity: critical }
        annotations:
          summary: "Database connection pool has waiting consumers"

      - alert: NotificationDeliveryFailing
        expr: rate(tailbook_notifications_delivery_attempts_total{status="failed"}[5m]) > 0
        for: 5m
        labels: { severity: warning }
        annotations:
          summary: "Notification delivery attempts are failing"

      - alert: AuditBatchWritesFailing
        expr: rate(tailbook_audit_batch_writes_total{result="failed"}[1m]) > 0
        for: 1m
        labels: { severity: critical }
        annotations:
          summary: "Audit batch writes are failing — audit trail may be incomplete"

      - alert: HealthCheckUnhealthy
        expr: tailbook_health_reports_total{status="unhealthy"} > 0
        for: 30s
        labels: { severity: critical }
        annotations:
          summary: "Health check report is unhealthy"

      - alert: UnhandledException
        expr: rate(tailbook_api_unhandled_exceptions_total[1m]) > 0
        for: 30s
        labels: { severity: critical }
        annotations:
          summary: "Unhandled API exceptions detected — check trace IDs in logs"
```

## Useful signal reference

### API host signals
- meter: `Tailbook.Api`
- request counter: `tailbook.api.http.server.requests`
- request histogram: `tailbook.api.http.server.duration`
- counter: `tailbook.api.unhandled_exceptions`
- health counters: `tailbook.health.reports`, `tailbook.health.checks`
- health histograms: `tailbook.health.report.duration`, `tailbook.health.check.duration`

### Job queue signals
- activity source: `Tailbook.Jobs`
- meter: `Tailbook.Jobs`
- spans: `jobs.storage.store`, `jobs.storage.dequeue`, `jobs.storage.complete`, `jobs.storage.cancel`, `jobs.storage.reschedule_failed`, `jobs.storage.purge_stale`, `jobs.storage.store_result`, `jobs.storage.get_result`
- counters: `tailbook.jobs.storage.operations`, `tailbook.jobs.storage.items`
- histogram: `tailbook.jobs.storage.operation.duration`

FastEndpoints job queue records are persisted in `public."Jobs"`. Repeated `FastEndpoints.JobQueue` storage retrieve errors usually mean the application has not run the migration that creates this table, or the API host is running with a stale build that does not include the `JobRecord` EF model mapping.

### Notification signals
- activity source: `Tailbook.Notifications`
- meter: `Tailbook.Notifications`
- span: `notifications.outbox.process`
- counters: `tailbook.notifications.outbox.cycles`, `tailbook.notifications.outbox.processed`, `tailbook.notifications.outbox.messages`, `tailbook.notifications.delivery.attempts`, `tailbook.notifications.background.failures`
- histogram: `tailbook.notifications.outbox.duration`

### Audit signals
- activity source: `Tailbook.Audit`
- meter: `Tailbook.Audit`
- counters: `tailbook.audit.queue.enqueued`, `tailbook.audit.queue.dequeued`, `tailbook.audit.batch.writes`, `tailbook.audit.batch.items`, `tailbook.audit.batch.retries`
- histograms: `tailbook.audit.queue.enqueue.duration`, `tailbook.audit.batch.duration`

### Outbox signals
- activity source: `Tailbook.Outbox`
- meter: `Tailbook.Outbox`
- span: `outbox.message.stage`
- counter: `tailbook.outbox.messages.staged`
- histogram: `tailbook.outbox.payload.size`

### Database signals (built-in Npgsql)
- meter: `Npgsql`
- gauges: `npgsql.connection_pool.active`, `npgsql.connection_pool.idle`, `npgsql.connection_pool.waiting`, `npgsql.connection_pool.max`

## Startup diagnostics

Startup logs include sanitized runtime shape:
- environment name
- database host and database name
- CORS origin count
- HTTPS redirection and HSTS flags
- notification background-processing state and poll interval
- staff scheduling time zone
- telemetry enabled state and whether OTLP export is configured
- whether OTLP log export is configured
- database pool name

Secrets and full connection strings must remain in environment variables or secret stores, not logs.

## Local observability stack

The local `docker-compose.yml` includes Prometheus and Grafana for visualising telemetry in development.

### Start the stack

```bash
docker compose up -d postgres redis otel-collector prometheus grafana
```

The API does not start automatically in the local Docker setup (it runs via `dotnet run` for hot reload). To route API telemetry to the collector, set the environment variable before starting the API:

```bash
# PowerShell
$env:Telemetry__OtlpEndpoint = "http://localhost:4317"
dotnet run --project backend/src/Tailbook.Api.Host

# Bash (WSL/Git Bash)
export Telemetry__OtlpEndpoint=http://localhost:4317
dotnet run --project backend/src/Tailbook.Api.Host
```

Alternatively, add the variable to your `.env` file:
```
Telemetry__OtlpEndpoint=http://localhost:4317
```

### Access dashboards

| Service | URL | Credentials |
|---|---|---|
| Grafana | http://localhost:3000 | `admin` / `tailbook-local` (configurable via `.env`) |
| Prometheus | http://localhost:9090 | none |
| OTel Collector | gRPC `localhost:4317` | none |

The "Tailbook API Overview" dashboard is auto-provisioned in Grafana. After starting the API with OTLP export, open Grafana and navigate to **Dashboards > Tailbook API Overview** to see request rate, latency, health status, exceptions, database pool, and notification delivery metrics.

### Verify telemetry flow

1. Start the observability stack: `docker compose up -d otel-collector prometheus grafana`
2. Start the API with `Telemetry__OtlpEndpoint=http://localhost:4317`
3. Make a few requests: `curl http://localhost:5001/health/ready`
4. Open Prometheus at http://localhost:9090 and query `tailbook_api_http_server_requests_total` — you should see counter values
5. Open Grafana at http://localhost:3000 and check the Tailbook API Overview dashboard

### Production observability setup

1. **Deploy an OTLP collector** — the production Docker Compose includes `otel-collector` that accepts OTLP gRPC on port 4317. The API is pre-configured to export to `http://otel-collector:4317` via the `Telemetry__OtlpEndpoint` environment variable.
2. **Forward telemetry upstream** — set `OTEL_COLLECTOR_EXPORTER_ENDPOINT` on the `otel-collector` service to route telemetry to your observability backend (e.g., Grafana Cloud, Datadog, or a central OTel Collector). If unset, the collector logs telemetry to stdout (useful for debugging).
3. **Configure dashboards** using the panel guidance above. Start with API HTTP latency/error-rate and database pool panels — these cover the most common failure modes.
4. **Enable log export** only after verifying traces and metrics arrive at the collector. Set `Telemetry__ExportLogs=true` (the default) to include structured logs.
5. **Verify X-Trace-Id end-to-end** by making a test request and confirming the trace ID appears in both the response header and the log aggregator.
