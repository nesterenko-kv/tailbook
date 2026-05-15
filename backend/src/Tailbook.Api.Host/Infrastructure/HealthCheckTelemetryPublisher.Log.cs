namespace Tailbook.Api.Host.Infrastructure;

public sealed partial class HealthCheckTelemetryPublisher
{
    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Debug,
        Message = "Health check report {HealthStatus} in {DurationMilliseconds} ms.")]
    public partial void LogHealthCheckReportHealthy(
        string healthStatus,
        double durationMilliseconds);

    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Warning,
        Message = "Health check report {HealthStatus} in {DurationMilliseconds} ms.")]
    public partial void LogHealthCheckReportNotHealthy(
        string healthStatus,
        double durationMilliseconds);

    [LoggerMessage(
        EventId = 1202,
        Level = LogLevel.Warning,
        Message =
            "Health check {HealthCheckName} reported {HealthStatus} in {DurationMilliseconds} ms with error type {ErrorType}.")]
    public partial void LogHealthCheckEntryNotHealthy(
        string healthCheckName,
        string healthStatus,
        double durationMilliseconds,
        string errorType);
}
