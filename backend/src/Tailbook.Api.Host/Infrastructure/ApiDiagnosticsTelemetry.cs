using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tailbook.Api.Host.Infrastructure;

public static class ApiDiagnosticsTelemetry
{
    public const string MeterName = "Tailbook.Api";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> UnhandledExceptions = Meter.CreateCounter<long>(
        "tailbook.api.unhandled_exceptions",
        description: "Unhandled API exceptions converted to sanitized problem responses.");
    private static readonly Counter<long> HttpRequests = Meter.CreateCounter<long>(
        "tailbook.api.http.server.requests",
        description: "Completed API HTTP requests.");
    private static readonly Histogram<double> HttpRequestDuration = Meter.CreateHistogram<double>(
        "tailbook.api.http.server.duration",
        "ms",
        "Completed API HTTP request duration.");
    private static readonly Counter<long> HealthReports = Meter.CreateCounter<long>(
        "tailbook.health.reports",
        description: "Published health check reports by status.");
    private static readonly Counter<long> HealthChecks = Meter.CreateCounter<long>(
        "tailbook.health.checks",
        description: "Published health check entries by status.");
    private static readonly Histogram<double> HealthReportDuration = Meter.CreateHistogram<double>(
        "tailbook.health.report.duration",
        "ms",
        "Published health check report duration.");
    private static readonly Histogram<double> HealthCheckDuration = Meter.CreateHistogram<double>(
        "tailbook.health.check.duration",
        "ms",
        "Published health check entry duration.");

    public static void RecordUnhandledException(string method, string route, string exceptionType)
    {
        var tags = new TagList
        {
            { "http.request.method", Normalize(method) },
            { "http.route", Normalize(route) },
            { "exception.type", Normalize(exceptionType) }
        };

        UnhandledExceptions.Add(1, tags);
    }

    public static void RecordHttpRequest(string method, string route, int statusCode, double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "http.request.method", Normalize(method) },
            { "http.route", Normalize(route) },
            { "http.response.status_code", statusCode },
            { "tailbook.http.status_class", GetStatusClass(statusCode) }
        };

        HttpRequests.Add(1, tags);
        HttpRequestDuration.Record(durationMilliseconds, tags);
    }

    public static string GetStatusClass(int statusCode)
    {
        return statusCode is >= 100 and <= 599
            ? $"{statusCode / 100}xx"
            : "unknown";
    }

    public static void RecordHealthReport(HealthReport report)
    {
        var reportTags = new TagList
        {
            { "tailbook.health.status", report.Status.ToString() }
        };

        HealthReports.Add(1, reportTags);
        HealthReportDuration.Record(report.TotalDuration.TotalMilliseconds, reportTags);

        foreach (var entry in report.Entries.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var entryTags = new TagList
            {
                { "tailbook.health.check", Normalize(entry.Key) },
                { "tailbook.health.status", entry.Value.Status.ToString() },
                { "exception.type", entry.Value.Exception?.GetType().Name ?? "none" }
            };

            HealthChecks.Add(1, entryTags);
            HealthCheckDuration.Record(entry.Value.Duration.TotalMilliseconds, entryTags);
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
