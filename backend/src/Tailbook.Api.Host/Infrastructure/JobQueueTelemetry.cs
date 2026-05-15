using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tailbook.Api.Host.Infrastructure;

public static class JobQueueTelemetry
{
    public const string ActivitySourceName = "Tailbook.Jobs";
    public const string MeterName = "Tailbook.Jobs";
    public const string ResultSuccess = "success";
    public const string ResultMiss = "miss";
    public const string ResultError = "error";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Operations = Meter.CreateCounter<long>(
        "tailbook.jobs.storage.operations",
        description: "FastEndpoints job storage operations.");
    private static readonly Counter<long> Items = Meter.CreateCounter<long>(
        "tailbook.jobs.storage.items",
        description: "FastEndpoints job storage items handled by operation.");
    private static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        "tailbook.jobs.storage.operation.duration",
        "ms",
        "FastEndpoints job storage operation duration.");

    public static Activity? StartStorageOperation(string operation, string? queueId = null)
    {
        var normalizedOperation = Normalize(operation);
        var activity = ActivitySource.StartActivity($"jobs.storage.{normalizedOperation}");
        activity?.SetTag("tailbook.jobs.operation", normalizedOperation);
        activity?.SetTag("tailbook.jobs.queue", Normalize(queueId));

        return activity;
    }

    public static void RecordStorageOperation(
        Activity? activity,
        string operation,
        string? queueId,
        int itemCount,
        TimeSpan duration,
        string result)
    {
        var normalizedOperation = Normalize(operation);
        var normalizedQueueId = Normalize(queueId);
        var normalizedResult = Normalize(result);

        activity?.SetTag("tailbook.jobs.operation", normalizedOperation);
        activity?.SetTag("tailbook.jobs.queue", normalizedQueueId);
        activity?.SetTag("tailbook.jobs.result", normalizedResult);
        activity?.SetTag("tailbook.jobs.item_count", itemCount);
        activity?.SetTag("tailbook.jobs.duration_ms", duration.TotalMilliseconds);

        var tags = new TagList
        {
            { "tailbook.jobs.operation", normalizedOperation },
            { "tailbook.jobs.queue", normalizedQueueId },
            { "tailbook.jobs.result", normalizedResult }
        };

        Operations.Add(1, tags);

        if (itemCount > 0)
        {
            Items.Add(itemCount, tags);
        }

        OperationDuration.Record(duration.TotalMilliseconds, tags);
    }

    public static void RecordStorageError(Activity? activity, Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
        activity?.AddException(exception);
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
}
