using System.Diagnostics;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Xunit;

namespace Tailbook.BuildingBlocks.Tests;

public sealed class RabbitMqTelemetryTests : IDisposable
{
    private readonly ActivityListener _listener;

    public RabbitMqTelemetryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Fact]
    public void StartPublishActivity_returns_activity_with_tags()
    {
        using var activity = RabbitMqTelemetry.StartPublishActivity("test.exchange", "test.key");

        Assert.NotNull(activity);
        Assert.Equal("messaging.publish", activity!.OperationName);
        Assert.Equal("rabbitmq", activity.GetTagItem("messaging.system"));
        Assert.Equal("test.exchange", activity.GetTagItem("messaging.destination"));
        Assert.Equal("test.key", activity.GetTagItem("messaging.rabbitmq.routing_key"));
    }

    [Fact]
    public void StartConsumeActivity_returns_activity_with_tags()
    {
        using var activity = RabbitMqTelemetry.StartConsumeActivity("test.exchange", "test.key");

        Assert.NotNull(activity);
        Assert.Equal("messaging.consume", activity!.OperationName);
        Assert.Equal("rabbitmq", activity.GetTagItem("messaging.system"));
        Assert.Equal("test.exchange", activity.GetTagItem("messaging.source"));
    }

    [Fact]
    public void RecordPublish_does_not_throw()
    {
        RabbitMqTelemetry.RecordPublish("e", "k", 100, TimeSpan.FromMilliseconds(5), success: true);
        RabbitMqTelemetry.RecordPublish("e", "k", 200, TimeSpan.FromMilliseconds(10), success: false);
    }

    [Fact]
    public void RecordConsume_does_not_throw()
    {
        RabbitMqTelemetry.RecordConsume("e", "k", success: true);
        RabbitMqTelemetry.RecordConsume("e", "k", success: false);
    }

    [Fact]
    public void Activity_source_and_meter_names_are_defined()
    {
        Assert.Equal("Tailbook.Messaging", RabbitMqTelemetry.ActivitySourceName);
        Assert.Equal("Tailbook.Messaging", RabbitMqTelemetry.MeterName);
    }
}
