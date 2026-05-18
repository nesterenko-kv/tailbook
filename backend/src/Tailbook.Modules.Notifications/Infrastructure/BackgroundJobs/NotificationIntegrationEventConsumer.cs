using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.Notifications.Infrastructure.Options;
using Tailbook.Modules.Notifications.Infrastructure.Services;
using Tailbook.Modules.Notifications.Infrastructure.Telemetry;

namespace Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;

public sealed class NotificationIntegrationEventConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly NotificationsOptions _notificationsOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationIntegrationEventConsumer> _logger;

    public NotificationIntegrationEventConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<NotificationsOptions> notificationsOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationIntegrationEventConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _notificationsOptions = notificationsOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_rabbitMqOptions.Enabled || !_notificationsOptions.EnableBackgroundProcessing)
        {
            return;
        }

        var exchange = _rabbitMqOptions.Exchange;
        var queue = "notifications";

        var channel = await _connectionFactory.CreateChannelAsync(stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: queue,
            exchange: exchange,
            routingKey: "#",
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            using var activity = RabbitMqTelemetry.StartConsumeActivity(exchange, args.RoutingKey);

            try
            {
                await ProcessIntegrationMessageAsync(args.Body, args.RoutingKey, args.BasicProperties, stoppingToken);

                await channel.BasicAckAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);

                RabbitMqTelemetry.RecordConsume(exchange, args.RoutingKey, success: true);
            }
            catch (Exception ex)
            {
                NotificationTelemetry.RecordBackgroundProcessingFailure();
                _logger.LogError(ex,
                    "Failed to process integration event for notifications from routing key {RoutingKey}.",
                    args.RoutingKey);

                RabbitMqTelemetry.RecordConsume(exchange, args.RoutingKey, success: false);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                await channel.BasicNackAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.NotificationConsumerStarted(queue, exchange);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.NotificationConsumerStopped();
        }
    }

    private async Task ProcessIntegrationMessageAsync(
        ReadOnlyMemory<byte> body,
        string routingKey,
        IReadOnlyBasicProperties? properties,
        CancellationToken cancellationToken)
    {
        var payloadJson = Encoding.UTF8.GetString(body.Span);
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var eventType = root.TryGetProperty("eventType", out var et)
            ? et.GetString()
            : null;

        var innerPayload = root.TryGetProperty("payloadJson", out var pj)
            ? pj.GetString()
            : null;

        var messageId = root.TryGetProperty("messageId", out var mid)
            ? mid.GetGuid()
            : Guid.TryParse(properties?.MessageId, out var parsed)
                ? parsed
                : Guid.NewGuid();

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(innerPayload))
        {
            _logger.LogWarning(
                "Received malformed integration event for notifications from routing key {RoutingKey}. Skipping.",
                routingKey);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var useCases = scope.ServiceProvider.GetRequiredService<NotificationUseCases>();

        var result = await useCases.ProcessBrokerNotificationAsync(
            eventType,
            innerPayload,
            messageId,
            cancellationToken);

        if (result.Outcome == "sent")
        {
            _logger.NotificationDispatchedFromIntegrationEvent(messageId, eventType);
        }
        else if (result.Outcome == "dead_letter")
        {
            _logger.LogWarning(
                "Notification from integration event {MessageId} ({EventType}) dead-lettered: {Error}",
                messageId, eventType, result.ErrorMessage);
        }
        else if (result.Outcome == "ignored")
        {
            _logger.LogDebug(
                "Integration event {MessageId} ({EventType}) ignored for notifications (no matching template).",
                messageId, eventType);
        }
    }
}
