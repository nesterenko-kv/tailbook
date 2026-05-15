using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tailbook.Api.Host.Infrastructure;

public sealed partial class HealthCheckTelemetryPublisher(ILogger<HealthCheckTelemetryPublisher> logger) : IHealthCheckPublisher
{
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        ApiDiagnosticsTelemetry.RecordHealthReport(report);

        var status = report.Status.ToString();
        var totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2);
        if (report.Status == HealthStatus.Healthy)
        {
            LogHealthCheckReportHealthy(status, totalDurationMs);
            return Task.CompletedTask;
        }

        LogHealthCheckReportNotHealthy(status, totalDurationMs);
        foreach (var entry in report.Entries.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (entry.Value.Status == HealthStatus.Healthy)
            {
                continue;
            }

            LogHealthCheckEntryNotHealthy(
                entry.Key,
                entry.Value.Status.ToString(),
                Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                entry.Value.Exception?.GetType().Name ?? "none");
        }

        return Task.CompletedTask;
    }
}
