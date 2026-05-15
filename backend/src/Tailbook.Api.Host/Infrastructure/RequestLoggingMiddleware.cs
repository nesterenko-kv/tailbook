using Serilog.Context;
using Tailbook.BuildingBlocks.Infrastructure.Diagnostics;

namespace Tailbook.Api.Host.Infrastructure;

public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = TraceContext.GetTraceId(context);
        var spanId = TraceContext.GetSpanId();
        using var traceIdProperty = LogContext.PushProperty("TraceId", traceId);
        using var spanIdProperty = string.IsNullOrWhiteSpace(spanId) ? null : LogContext.PushProperty("SpanId", spanId);
        var sw = ValueStopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? string.Empty;
            var statusCode = context.Response.StatusCode;
            var elapsedMilliseconds = sw.GetElapsedMilliseconds();
            var route = GetRoutePattern(context);

            ApiDiagnosticsTelemetry.RecordHttpRequest(method, route, statusCode, elapsedMilliseconds);

            LogHttpRequestCompleted(
                method,
                path,
                statusCode,
                elapsedMilliseconds,
                traceId,
                spanId ?? string.Empty
            );
        }
    }

    private static string GetRoutePattern(HttpContext context)
    {
        return context.GetEndpoint() is RouteEndpoint routeEndpoint
            ? routeEndpoint.RoutePattern.RawText ?? "unknown"
            : "unknown";
    }
}
