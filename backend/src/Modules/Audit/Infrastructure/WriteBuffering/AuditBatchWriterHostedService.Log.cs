using Microsoft.Extensions.Logging;

namespace Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

internal static partial class AuditBatchWriterHostedServiceMessages
{
    [LoggerMessage(
        EventId = 9100,
        Level = LogLevel.Information,
        Message = "Audit batch writer started with batch size {BatchSize}, flush interval {FlushIntervalMilliseconds} ms, and retry limit {MaxWriteRetries}.")]
    public static partial void AuditBatchWriterStarted(
        this ILogger logger,
        int batchSize,
        int flushIntervalMilliseconds,
        int maxWriteRetries);

    [LoggerMessage(
        EventId = 9101,
        Level = LogLevel.Debug,
        Message = "Audit batch persisted {ItemCount} item(s): {AccessAuditCount} access audit, {AuditTrailCount} audit trail in {DurationMilliseconds} ms.")]
    public static partial void AuditBatchPersisted(
        this ILogger logger,
        int itemCount,
        int accessAuditCount,
        int auditTrailCount,
        double durationMilliseconds);

    [LoggerMessage(
        EventId = 9102,
        Level = LogLevel.Warning,
        Message = "Audit batch write attempt {Attempt} of {MaxAttempts} failed for {ItemCount} item(s).")]
    public static partial void AuditBatchWriteAttemptFailed(
        this ILogger logger,
        Exception exception,
        int attempt,
        int maxAttempts,
        int itemCount);

    [LoggerMessage(
        EventId = 9103,
        Level = LogLevel.Error,
        Message = "Audit batch write failed after {MaxAttempts} attempt(s). Dropping {ItemCount} best-effort audit item(s).")]
    public static partial void AuditBatchWriteDropped(
        this ILogger logger,
        Exception exception,
        int maxAttempts,
        int itemCount);

    [LoggerMessage(
        EventId = 9104,
        Level = LogLevel.Information,
        Message = "Audit batch writer draining queued items during shutdown.")]
    public static partial void AuditBatchWriterDraining(this ILogger logger);
}
