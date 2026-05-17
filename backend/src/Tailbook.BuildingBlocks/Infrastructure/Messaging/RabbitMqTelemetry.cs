using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

public static class RabbitMqTelemetry
{
    public const string ActivitySourceName = "Tailbook.Messaging";
    public const string MeterName = "Tailbook.Messaging";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> MessagesPublished = Meter.CreateCounter<long>(
        "tailbook.messaging.published",
        description: "Messages published to the message broker.");

    private static readonly Counter<long> MessagesConsumed = Meter.CreateCounter<long>(
        "tailbook.messaging.consumed",
        description: "Messages consumed from the message broker.");

    private static readonly Counter<long> PublishFailures = Meter.CreateCounter<long>(
        "tailbook.messaging.publish.failures",
        description: "Failed message publish attempts.");

    private static readonly Counter<long> ConsumeFailures = Meter.CreateCounter<long>(
        "tailbook.messaging.consume.failures",
        description: "Failed message consume attempts.");

    private static readonly Histogram<double> PublishDuration = Meter.CreateHistogram<double>(
        "tailbook.messaging.publish.duration",
        "ms",
        "Message publish duration.");

    public static Activity? StartPublishActivity(string exchange, string routingKey)
    {
        var activity = ActivitySource.StartActivity("messaging.publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", Normalize(exchange));
        activity?.SetTag("messaging.destination_kind", "exchange");
        activity?.SetTag("messaging.rabbitmq.routing_key", Normalize(routingKey));
        return activity;
    }

    public static Activity? StartConsumeActivity(string exchange, string routingKey)
    {
        var activity = ActivitySource.StartActivity("messaging.consume", ActivityKind.Consumer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.source", Normalize(exchange));
        activity?.SetTag("messaging.source_kind", "exchange");
        activity?.SetTag("messaging.rabbitmq.routing_key", Normalize(routingKey));
        return activity;
    }

    public static void RecordPublish(string exchange, string routingKey, long payloadSizeBytes, TimeSpan duration, bool success)
    {
        var tags = new TagList
        {
            { "tailbook.messaging.destination", Normalize(exchange) },
            { "tailbook.messaging.routing_key", Normalize(routingKey) },
            { "tailbook.messaging.success", success }
        };

        MessagesPublished.Add(1, tags);
        PublishDuration.Record(duration.TotalMilliseconds, tags);

        if (!success)
        {
            PublishFailures.Add(1, tags);
        }
    }

    public static void RecordConsume(string exchange, string routingKey, bool success)
    {
        var tags = new TagList
        {
            { "tailbook.messaging.source", Normalize(exchange) },
            { "tailbook.messaging.routing_key", Normalize(routingKey) },
            { "tailbook.messaging.success", success }
        };

        MessagesConsumed.Add(1, tags);

        if (!success)
        {
            ConsumeFailures.Add(1, tags);
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
