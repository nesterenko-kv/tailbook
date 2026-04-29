using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tailbook.Api.Host.Infrastructure;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    durationMs = Math.Round(x.Value.Duration.TotalMilliseconds, 2),
                    tags = x.Value.Tags.OrderBy(tag => tag, StringComparer.Ordinal).ToArray(),
                    errorType = x.Value.Exception?.GetType().Name
                })
                .ToArray()
        };

        return JsonSerializer.SerializeAsync(context.Response.Body, payload, JsonOptions);
    }
}
