using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tailbook.Modules.Audit.Infrastructure.Telemetry;

public static class AuditTelemetry
{
    public const string ActivitySourceName = "Tailbook.Audit";
    public const string MeterName = "Tailbook.Audit";
    public const string BatchWriteActivityName = "audit.batch.write";
    public const string ResultSuccess = "success";
    public const string ResultError = "error";
    public const string ResultCanceled = "canceled";
    public const string ResultDropped = "dropped";
    public const string ResultAccepted = "accepted";
    public const string ItemTypeAccessAudit = "access_audit";
    public const string ItemTypeAuditTrail = "audit_trail";
    public const string ItemTypeMixed = "mixed";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> QueueEnqueued = Meter.CreateCounter<long>(
        "tailbook.audit.queue.enqueued",
        description: "Audit write items accepted into the in-memory queue.");
    private static readonly Counter<long> QueueDequeued = Meter.CreateCounter<long>(
        "tailbook.audit.queue.dequeued",
        description: "Audit write items removed from the in-memory queue by the batch writer.");
    private static readonly Histogram<double> EnqueueDuration = Meter.CreateHistogram<double>(
        "tailbook.audit.queue.enqueue.duration",
        "ms",
        "Audit queue enqueue duration.");
    private static readonly Counter<long> BatchWrites = Meter.CreateCounter<long>(
        "tailbook.audit.batch.writes",
        description: "Audit batch write attempts by result.");
    private static readonly Counter<long> BatchItems = Meter.CreateCounter<long>(
        "tailbook.audit.batch.items",
        description: "Audit write items handled by batch result.");
    private static readonly Counter<long> BatchRetries = Meter.CreateCounter<long>(
        "tailbook.audit.batch.retries",
        description: "Audit batch write retries.");
    private static readonly Histogram<double> BatchDuration = Meter.CreateHistogram<double>(
        "tailbook.audit.batch.duration",
        "ms",
        "Audit batch persistence duration.");

    public static Activity? StartBatchWriteActivity(int itemCount, int accessAuditCount, int auditTrailCount)
    {
        var activity = ActivitySource.StartActivity(BatchWriteActivityName);
        activity?.SetTag("tailbook.audit.batch.item_count", itemCount);
        activity?.SetTag("tailbook.audit.batch.access_audit_count", accessAuditCount);
        activity?.SetTag("tailbook.audit.batch.audit_trail_count", auditTrailCount);
        activity?.SetTag("tailbook.audit.item_type", GetBatchItemType(accessAuditCount, auditTrailCount));
        return activity;
    }

    public static void RecordQueueEnqueued(string itemType, TimeSpan duration, string result)
    {
        var tags = new TagList
        {
            { "tailbook.audit.item_type", Normalize(itemType) },
            { "tailbook.audit.result", Normalize(result) }
        };

        if (result == ResultAccepted)
        {
            QueueEnqueued.Add(1, tags);
        }

        EnqueueDuration.Record(duration.TotalMilliseconds, tags);
    }

    public static void RecordQueueDequeued(string itemType)
    {
        var tags = new TagList
        {
            { "tailbook.audit.item_type", Normalize(itemType) }
        };

        QueueDequeued.Add(1, tags);
    }

    public static void RecordBatchWrite(
        Activity? activity,
        int itemCount,
        int accessAuditCount,
        int auditTrailCount,
        TimeSpan duration,
        string result)
    {
        var itemType = GetBatchItemType(accessAuditCount, auditTrailCount);
        var normalizedResult = Normalize(result);

        activity?.SetTag("tailbook.audit.batch.item_count", itemCount);
        activity?.SetTag("tailbook.audit.batch.access_audit_count", accessAuditCount);
        activity?.SetTag("tailbook.audit.batch.audit_trail_count", auditTrailCount);
        activity?.SetTag("tailbook.audit.duration_ms", duration.TotalMilliseconds);
        activity?.SetTag("tailbook.audit.result", normalizedResult);
        activity?.SetTag("tailbook.audit.item_type", itemType);

        if (normalizedResult == ResultError)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }

        var tags = new TagList
        {
            { "tailbook.audit.item_type", itemType },
            { "tailbook.audit.result", normalizedResult }
        };

        BatchWrites.Add(1, tags);
        if (itemCount > 0)
        {
            BatchItems.Add(itemCount, tags);
        }

        BatchDuration.Record(duration.TotalMilliseconds, tags);
    }

    public static void RecordBatchRetry(int attempt)
    {
        var tags = new TagList
        {
            { "tailbook.audit.attempt", attempt }
        };

        BatchRetries.Add(1, tags);
    }

    public static void RecordBatchException(Activity? activity, Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
        activity?.AddException(exception);
    }

    private static string GetBatchItemType(int accessAuditCount, int auditTrailCount)
    {
        return (accessAuditCount, auditTrailCount) switch
        {
            (> 0, > 0) => ItemTypeMixed,
            (> 0, _) => ItemTypeAccessAudit,
            (_, > 0) => ItemTypeAuditTrail,
            _ => "empty"
        };
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
