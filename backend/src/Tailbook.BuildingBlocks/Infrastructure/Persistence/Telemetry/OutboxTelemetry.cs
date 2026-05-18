using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;

public static class OutboxTelemetry
{
    public const string ActivitySourceName = "Tailbook.Outbox";
    public const string MeterName = "Tailbook.Outbox";
    public const string MessageStagedActivityName = "outbox.message.stage";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> MessagesStaged = Meter.CreateCounter<long>(
        "tailbook.outbox.messages.staged",
        description: "Outbox messages staged on the current unit of work.");
    private static readonly Histogram<long> PayloadSize = Meter.CreateHistogram<long>(
        "tailbook.outbox.payload.size",
        "By",
        "Serialized outbox payload size.");
    private static readonly Counter<long> PublisherFailures = Meter.CreateCounter<long>(
        "tailbook.outbox.publisher.failures",
        description: "Unhandled integration outbox publisher failures.");

    private static readonly Counter<long> MessagesPublished = Meter.CreateCounter<long>(
        "tailbook.outbox.messages.published",
        description: "Outbox messages successfully published to the message broker.");
    private static readonly Counter<long> MessagesPoisoned = Meter.CreateCounter<long>(
        "tailbook.outbox.messages.poisoned",
        description: "Outbox messages moved to dead-letter/poison state.");
    private static readonly Histogram<long> RetryDepth = Meter.CreateHistogram<long>(
        "tailbook.outbox.messages.retry_depth",
        description: "Distribution of retry counts for outbox messages.");

    private static readonly ConcurrentDictionary<string, double> s_moduleLagSeconds = new();
    private static readonly ObservableGauge<double> LagSeconds = Meter.CreateObservableGauge(
        "tailbook.outbox.lag.seconds",
        () => s_moduleLagSeconds.Select(kvp => new Measurement<double>(
            kvp.Value,
            new TagList { { "tailbook.outbox.module", Normalize(kvp.Key) } })),
        description: "Time between OccurredAt and now for oldest unprocessed message per module.");

    private static double s_oldestUnprocessedAgeSeconds;
    private static readonly ObservableGauge<double> OldestUnprocessedAgeSeconds = Meter.CreateObservableGauge(
        "tailbook.outbox.oldest_unprocessed_age_seconds",
        () => s_oldestUnprocessedAgeSeconds,
        description: "Age of the oldest unprocessed (non-poisoned) message in seconds.");

    public static Activity? StartMessageStagedActivity(
        string moduleCode,
        string eventType,
        Guid messageId,
        long payloadSizeBytes)
    {
        var activity = ActivitySource.StartActivity(MessageStagedActivityName, ActivityKind.Producer);
        activity?.SetTag("messaging.system", "tailbook.outbox");
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("tailbook.outbox.module", Normalize(moduleCode));
        activity?.SetTag("tailbook.outbox.event_type", Normalize(eventType));
        activity?.SetTag("tailbook.outbox.message_id", messageId.ToString("D"));
        activity?.SetTag("tailbook.outbox.payload_size_bytes", payloadSizeBytes);
        return activity;
    }

    public static void RecordMessageStaged(string moduleCode, string eventType, long payloadSizeBytes)
    {
        var tags = new TagList
        {
            { "tailbook.outbox.module", Normalize(moduleCode) },
            { "tailbook.outbox.event_type", Normalize(eventType) }
        };

        MessagesStaged.Add(1, tags);
        PayloadSize.Record(payloadSizeBytes, tags);
    }

    public static void RecordPublisherFailure()
    {
        PublisherFailures.Add(1);
    }

    public static void RecordMessagePublished(string moduleCode, string eventType)
    {
        var tags = new TagList
        {
            { "tailbook.outbox.module", Normalize(moduleCode) },
            { "tailbook.outbox.event_type", Normalize(eventType) }
        };

        MessagesPublished.Add(1, tags);
    }

    public static void RecordMessagePoisoned(string moduleCode, string eventType)
    {
        var tags = new TagList
        {
            { "tailbook.outbox.module", Normalize(moduleCode) },
            { "tailbook.outbox.event_type", Normalize(eventType) }
        };

        MessagesPoisoned.Add(1, tags);
    }

    public static void RecordRetryDepth(string moduleCode, int retryCount)
    {
        var tags = new TagList
        {
            { "tailbook.outbox.module", Normalize(moduleCode) }
        };

        RetryDepth.Record(retryCount, tags);
    }

    public static void RecordModuleLag(string moduleCode, double lagSeconds)
    {
        s_moduleLagSeconds[Normalize(moduleCode)] = lagSeconds;
    }

    public static void ClearModuleLag(string moduleCode)
    {
        s_moduleLagSeconds.TryRemove(Normalize(moduleCode), out _);
    }

    public static void RecordOldestUnprocessedAge(double ageSeconds)
    {
        s_oldestUnprocessedAgeSeconds = ageSeconds;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
