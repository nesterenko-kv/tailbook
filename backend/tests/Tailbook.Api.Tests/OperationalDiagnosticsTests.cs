using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tailbook.Api.Host.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Jobs;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class OperationalDiagnosticsTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Readiness_health_check_returns_structured_status_without_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.True(document.RootElement.TryGetProperty("totalDurationMs", out _));

        var checks = document.RootElement.GetProperty("checks").EnumerateArray().ToArray();
        Assert.Contains(checks, check => check.GetProperty("name").GetString() == "postgresql");
        Assert.All(checks, check => Assert.True(check.TryGetProperty("errorType", out _)));
    }

    [Fact]
    public async Task Responses_include_trace_id_header_for_log_correlation()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Trace-Id", out var values));
        Assert.Contains(values, value => !string.IsNullOrWhiteSpace(value));
    }

    [Fact]
    public void Trace_context_prefers_activity_trace_id_for_opentelemetry_correlation()
    {
        using var activity = new Activity("tailbook-test").Start();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "fallback-trace-id"
        };

        var traceId = TraceContext.GetTraceId(context);

        Assert.Equal(activity.TraceId.ToString(), traceId);
    }

    [Fact]
    public void Trace_context_falls_back_to_http_context_trace_identifier_without_activity()
    {
        var previousActivity = Activity.Current;
        Activity.Current = null;
        try
        {
            var context = new DefaultHttpContext
            {
                TraceIdentifier = "fallback-trace-id"
            };

            var traceId = TraceContext.GetTraceId(context);

            Assert.Equal("fallback-trace-id", traceId);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [Theory]
    [InlineData(200, "2xx")]
    [InlineData(404, "4xx")]
    [InlineData(500, "5xx")]
    [InlineData(99, "unknown")]
    [InlineData(600, "unknown")]
    public void Api_diagnostics_maps_status_codes_to_status_classes(int statusCode, string expectedStatusClass)
    {
        Assert.Equal(expectedStatusClass, ApiDiagnosticsTelemetry.GetStatusClass(statusCode));
    }

    [Fact]
    public async Task Request_logging_middleware_records_request_count_and_duration_metrics()
    {
        var requestCount = 0L;
        var durationCount = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ApiDiagnosticsTelemetry.MeterName
                && (instrument.Name == "tailbook.api.http.server.requests"
                    || instrument.Name == "tailbook.api.http.server.duration"))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "tailbook.api.http.server.requests"
                && HasTag(tags, "http.request.method", "GET")
                && HasTag(tags, "http.response.status_code", 204)
                && HasTag(tags, "tailbook.http.status_class", "2xx"))
            {
                requestCount += measurement;
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "tailbook.api.http.server.duration"
                && HasTag(tags, "http.request.method", "GET")
                && HasTag(tags, "http.response.status_code", 204)
                && HasTag(tags, "tailbook.http.status_class", "2xx"))
            {
                durationCount++;
            }
        });
        listener.Start();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        var middleware = new RequestLoggingMiddleware(
            nextContext =>
            {
                nextContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        listener.RecordObservableInstruments();

        Assert.Equal(1, requestCount);
        Assert.Equal(1, durationCount);
    }

    [Fact]
    public async Task Unhandled_exception_middleware_returns_sanitized_problem_details_with_trace_id()
    {
        using var activity = new Activity("tailbook-test").Start();
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "fallback-trace-id"
        };
        context.Response.Body = new MemoryStream();
        var middleware = new UnhandledExceptionMiddleware(
            _ => throw new InvalidOperationException("sensitive failure detail"),
            NullLogger<UnhandledExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.True(context.Response.Headers.TryGetValue(TraceContext.TraceIdHeaderName, out var headerValue));
        Assert.Equal(activity.TraceId.ToString(), headerValue.ToString());
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        context.Response.Body.Position = 0;
        using var document = JsonDocument.Parse(context.Response.Body);
        var body = document.RootElement.GetRawText();
        Assert.Equal("An unexpected error occurred.", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(StatusCodes.Status500InternalServerError, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(activity.TraceId.ToString(), document.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain("sensitive failure detail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nameof(InvalidOperationException), body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Health_check_telemetry_publisher_logs_unhealthy_entries_without_exception_messages()
    {
        var logger = new CapturingLogger<HealthCheckTelemetryPublisher>();
        var publisher = new HealthCheckTelemetryPublisher(logger);
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["postgresql"] = new(
                    HealthStatus.Unhealthy,
                    description: null,
                    duration: TimeSpan.FromMilliseconds(12),
                    exception: new InvalidOperationException("password=secret"),
                    data: null)
            },
            TimeSpan.FromMilliseconds(15));

        await publisher.PublishAsync(report, CancellationToken.None);

        var messages = string.Join(Environment.NewLine, logger.Entries.Select(x => x.Message));
        Assert.Contains(logger.Entries, x => x.Level == LogLevel.Warning && x.Message.Contains("postgresql", StringComparison.Ordinal));
        Assert.Contains(nameof(InvalidOperationException), messages, StringComparison.Ordinal);
        Assert.DoesNotContain("password=secret", messages, StringComparison.OrdinalIgnoreCase);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }

    [Fact]
    public async Task Outbox_publisher_records_activity_tags_without_payload_values()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"outbox-telemetry-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = TestModelConfiguration.CreateDbContext(options);
        var publisher = new OutboxPublisher(dbContext, TimeProvider.System);
        Activity? stoppedActivity = null;
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == OutboxTelemetry.ActivitySourceName;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = activity => stoppedActivity = activity;
        ActivitySource.AddActivityListener(listener);

        await publisher.PublishAsync(
            "booking",
            "AppointmentCreated",
            new { appointmentId = "apt_secret_123" },
            CancellationToken.None);

        Assert.NotNull(stoppedActivity);
        Assert.Equal(OutboxTelemetry.MessageStagedActivityName, stoppedActivity!.OperationName);
        Assert.Equal(ActivityKind.Producer, stoppedActivity.Kind);
        Assert.Equal("tailbook.outbox", stoppedActivity.GetTagItem("messaging.system"));
        Assert.Equal("publish", stoppedActivity.GetTagItem("messaging.operation"));
        Assert.Equal("booking", stoppedActivity.GetTagItem("tailbook.outbox.module"));
        Assert.Equal("AppointmentCreated", stoppedActivity.GetTagItem("tailbook.outbox.event_type"));
        Assert.NotNull(stoppedActivity.GetTagItem("tailbook.outbox.message_id"));
        Assert.NotNull(stoppedActivity.GetTagItem("tailbook.outbox.payload_size_bytes"));
        Assert.DoesNotContain("apt_secret_123", string.Join(" ", stoppedActivity.Tags.Select(x => x.Value)), StringComparison.Ordinal);
    }

    [Fact]
    public void Job_queue_storage_telemetry_records_activity_and_metrics_without_tracking_ids()
    {
        Activity? stoppedActivity = null;
        var operationCount = 0L;
        var itemCount = 0L;
        var durationCount = 0;
        using var activityListener = new ActivityListener();
        activityListener.ShouldListenTo = source => source.Name == JobQueueTelemetry.ActivitySourceName;
        activityListener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        activityListener.ActivityStopped = activity => stoppedActivity = activity;
        ActivitySource.AddActivityListener(activityListener);
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == JobQueueTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (!HasTag(tags, "tailbook.jobs.operation", "store")
                || !HasTag(tags, "tailbook.jobs.queue", "default")
                || !HasTag(tags, "tailbook.jobs.result", JobQueueTelemetry.ResultSuccess))
            {
                return;
            }

            if (instrument.Name == "tailbook.jobs.storage.operations")
            {
                operationCount += measurement;
            }

            if (instrument.Name == "tailbook.jobs.storage.items")
            {
                itemCount += measurement;
            }
        });
        meterListener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "tailbook.jobs.storage.operation.duration"
                && HasTag(tags, "tailbook.jobs.operation", "store")
                && HasTag(tags, "tailbook.jobs.queue", "default")
                && HasTag(tags, "tailbook.jobs.result", JobQueueTelemetry.ResultSuccess))
            {
                durationCount++;
            }
        });
        meterListener.Start();

        using (var activity = JobQueueTelemetry.StartStorageOperation("store", "default"))
        {
            JobQueueTelemetry.RecordStorageOperation(
                activity,
                "store",
                "default",
                2,
                TimeSpan.FromMilliseconds(4),
                JobQueueTelemetry.ResultSuccess);
        }

        Assert.NotNull(stoppedActivity);
        Assert.Equal("jobs.storage.store", stoppedActivity!.OperationName);
        Assert.Equal(ActivityKind.Internal, stoppedActivity.Kind);
        Assert.Equal("store", stoppedActivity.GetTagItem("tailbook.jobs.operation"));
        Assert.Equal("default", stoppedActivity.GetTagItem("tailbook.jobs.queue"));
        Assert.Equal(JobQueueTelemetry.ResultSuccess, stoppedActivity.GetTagItem("tailbook.jobs.result"));
        Assert.Equal(2, stoppedActivity.GetTagItem("tailbook.jobs.item_count"));
        Assert.NotNull(stoppedActivity.GetTagItem("tailbook.jobs.duration_ms"));
        Assert.DoesNotContain(stoppedActivity.TagObjects, tag => tag.Key.Contains("tracking", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, operationCount);
        Assert.Equal(2, itemCount);
        Assert.Equal(1, durationCount);
    }

    [Fact]
    public void App_db_context_model_includes_fastendpoints_job_storage_records()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"job-storage-model-{Guid.NewGuid():N}")
            .Options;
        using var dbContext = TestModelConfiguration.CreateDbContext(options);

        var entityType = dbContext.Model.FindEntityType(typeof(JobRecord));

        Assert.NotNull(entityType);
        Assert.NotNull(dbContext.Set<JobRecord>());
        Assert.Equal("Jobs", entityType.GetTableName());
        Assert.Equal("public", entityType.GetSchema());
        Assert.NotNull(entityType.FindProperty(nameof(JobRecord.DequeueAfter)));
        Assert.NotNull(entityType.FindProperty(nameof(JobRecord.CommandJson)));
        Assert.NotNull(entityType.FindProperty(nameof(JobRecord.ResultJson)));
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private static bool HasTag<T>(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key, T value)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key && Equals(tag.Value, value))
            {
                return true;
            }
        }

        return false;
    }
}
