using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Telemetry;
using Tailbook.Modules.Notifications.Infrastructure.Options;

namespace Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;

public sealed class NotificationIntegrationEventConsumer : IntegrationEventConsumerBase
{
    private readonly ILogger<NotificationIntegrationEventConsumer> _logger;

    protected override string QueueName => "notifications";
    protected override string[] RoutingKeys => ["#"];
    protected override bool IsConsumerEnabled => _notificationsOptions.EnableBackgroundProcessing;

    private readonly NotificationsOptions _notificationsOptions;

    public NotificationIntegrationEventConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<NotificationsOptions> notificationsOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationIntegrationEventConsumer> logger
    ) : base(connectionFactory, rabbitMqOptions, scopeFactory, logger)
    {
        _notificationsOptions = notificationsOptions.Value;
        _logger = logger;
    }

    protected override Activity? StartConsumerActivity(string exchange, string routingKey)
    {
        return RabbitMqTelemetry.StartConsumeActivity(exchange, routingKey);
    }

    protected override void RecordConsumerResult(string exchange, string routingKey, bool success)
    {
        RabbitMqTelemetry.RecordConsume(exchange, routingKey, success);
    }

    protected override async Task ProcessEventAsync(
        string eventType,
        string innerPayload,
        Guid messageId,
        string routingKey,
        CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var inboxStore = scope.ServiceProvider.GetRequiredService<IInboxStore>();
        var messageIdStr = messageId.ToString("D");
        const string consumerName = "notifications";

        var received = await inboxStore.TryReceiveAsync(messageIdStr, consumerName, eventType, innerPayload, cancellationToken);

        if (received)
        {
            InboxTelemetry.RecordReceived(consumerName);
            _logger.LogDebug("Integration event {MessageId} ({EventType}) received into inbox for consumer {Consumer}.", messageId, eventType, consumerName);
        }
        else
        {
            _logger.LogDebug("Integration event {MessageId} ({EventType}) already in inbox for consumer {Consumer}, skipping.", messageId, eventType, consumerName);
        }
    }
}
