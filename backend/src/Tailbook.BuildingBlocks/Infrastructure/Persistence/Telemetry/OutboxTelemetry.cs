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

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
