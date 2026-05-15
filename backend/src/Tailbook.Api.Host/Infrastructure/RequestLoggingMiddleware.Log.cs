namespace Tailbook.Api.Host.Infrastructure;

public sealed partial class RequestLoggingMiddleware
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms with trace {TraceId} span {SpanId}")]
    partial void LogHttpRequestCompleted(
        string method,
        string path,
        int statusCode,
        double elapsedMilliseconds,
        string traceId,
        string spanId
    );
}
