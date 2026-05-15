using System.Diagnostics;

namespace Tailbook.Api.Host.Infrastructure;

public static class TraceContext
{
    public const string TraceIdHeaderName = "X-Trace-Id";

    public static string GetTraceId(HttpContext context)
    {
        var activityTraceId = Activity.Current?.TraceId.ToString();
        return string.IsNullOrWhiteSpace(activityTraceId) ? context.TraceIdentifier : activityTraceId;
    }

    public static string? GetSpanId()
    {
        var activitySpanId = Activity.Current?.SpanId.ToString();
        return string.IsNullOrWhiteSpace(activitySpanId) ? null : activitySpanId;
    }
}
