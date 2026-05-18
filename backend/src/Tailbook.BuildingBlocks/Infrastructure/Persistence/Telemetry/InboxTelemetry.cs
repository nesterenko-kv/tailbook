using System.Diagnostics.Metrics;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;

public static class InboxTelemetry
{
    public const string MeterName = "Tailbook.Inbox";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> MessagesReceived = Meter.CreateCounter<long>(
        "tailbook.inbox.messages.received",
        description: "Messages received into the inbox.");

    private static readonly Counter<long> MessagesCompleted = Meter.CreateCounter<long>(
        "tailbook.inbox.messages.completed",
        description: "Messages successfully processed from the inbox.");

    private static readonly Counter<long> MessagesFailed = Meter.CreateCounter<long>(
        "tailbook.inbox.messages.failed",
        description: "Messages that failed processing from the inbox.");

    private static readonly Counter<long> MessagesPoisoned = Meter.CreateCounter<long>(
        "tailbook.inbox.messages.poisoned",
        description: "Messages moved to poisoned state in the inbox.");

    private static readonly Histogram<long> RetryDepth = Meter.CreateHistogram<long>(
        "tailbook.inbox.messages.retry_depth",
        description: "Distribution of retry counts for inbox messages.");

    public static void RecordReceived(string consumerName)
    {
        MessagesReceived.Add(1, new KeyValuePair<string, object?>("consumer", consumerName));
    }

    public static void RecordCompleted(string consumerName)
    {
        MessagesCompleted.Add(1, new KeyValuePair<string, object?>("consumer", consumerName));
    }

    public static void RecordFailed(string consumerName)
    {
        MessagesFailed.Add(1, new KeyValuePair<string, object?>("consumer", consumerName));
    }

    public static void RecordPoisoned(string consumerName)
    {
        MessagesPoisoned.Add(1, new KeyValuePair<string, object?>("consumer", consumerName));
    }

    public static void RecordRetryDepth(string consumerName, int retryCount)
    {
        RetryDepth.Record(retryCount, new KeyValuePair<string, object?>("consumer", consumerName));
    }
}
