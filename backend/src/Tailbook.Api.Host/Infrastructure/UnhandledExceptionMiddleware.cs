using System.Diagnostics;
using System.Text.Json;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class UnhandledExceptionMiddleware(RequestDelegate next, ILogger<UnhandledExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers.TryAdd(TraceContext.TraceIdHeaderName, TraceContext.GetTraceId(context));

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                new
                {
                    type = "about:blank",
                    title = "The resource was modified by another request. Reload and retry.",
                    status = StatusCodes.Status409Conflict,
                    traceId = TraceContext.GetTraceId(context)
                },
                JsonOptions,
                context.RequestAborted);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.UnhandledRequestExceptionAfterResponseStarted(ex);
                throw;
            }

            var traceId = TraceContext.GetTraceId(context);
            var spanId = TraceContext.GetSpanId();
            var exceptionType = ex.GetType().Name;
            var route = GetRoutePattern(context);

            Activity.Current?.SetStatus(ActivityStatusCode.Error, exceptionType);
            Activity.Current?.AddException(ex);
            Activity.Current?.SetTag("tailbook.error.trace_id", traceId);
            ApiDiagnosticsTelemetry.RecordUnhandledException(context.Request.Method, route, exceptionType);

            logger.UnhandledRequestException(
                ex,
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                route,
                StatusCodes.Status500InternalServerError,
                traceId,
                spanId ?? string.Empty,
                exceptionType);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers.TryAdd(TraceContext.TraceIdHeaderName, traceId);

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                new
                {
                    type = "about:blank",
                    title = "An unexpected error occurred.",
                    status = StatusCodes.Status500InternalServerError,
                    traceId
                },
                JsonOptions,
                context.RequestAborted);
        }
    }

    private static string GetRoutePattern(HttpContext context)
    {
        return context.GetEndpoint() is RouteEndpoint routeEndpoint
            ? routeEndpoint.RoutePattern.RawText ?? "unknown"
            : "unknown";
    }
}

internal static partial class UnhandledExceptionMessages
{
    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Error,
        Message = "Unhandled API exception {ExceptionType} for {Method} {Path} route {Route} responded {StatusCode} with trace {TraceId} span {SpanId}")]
    public static partial void UnhandledRequestException(
        this ILogger logger,
        Exception exception,
        string method,
        string path,
        string route,
        int statusCode,
        string traceId,
        string spanId,
        string exceptionType);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Error,
        Message = "Unhandled API exception occurred after the response had already started.")]
    public static partial void UnhandledRequestExceptionAfterResponseStarted(this ILogger logger, Exception exception);
}
