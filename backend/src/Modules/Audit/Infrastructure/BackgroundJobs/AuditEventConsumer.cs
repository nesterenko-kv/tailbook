using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

namespace Tailbook.Modules.Audit.Infrastructure.BackgroundJobs;

public sealed class AuditEventConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditEventConsumer> _logger;

    public AuditEventConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditEventConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_rabbitMqOptions.Enabled)
        {
            return;
        }

        var exchange = _rabbitMqOptions.Exchange;
        var queue = "audit";

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
            try
            {
                await ProcessEventAsync(args.Body, args.RoutingKey, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process audit event from routing key {RoutingKey}.",
                    args.RoutingKey);
                await channel.BasicNackAsync(args.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.AuditEventConsumerStarted(queue, exchange);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.AuditEventConsumerStopped();
        }
    }

    private async Task ProcessEventAsync(ReadOnlyMemory<byte> body, string routingKey, CancellationToken cancellationToken)
    {
        var payloadJson = Encoding.UTF8.GetString(body.Span);
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
        var messageId = root.TryGetProperty("messageId", out var mid) ? mid.GetGuid() : (Guid?)null;
        var innerPayload = root.TryGetProperty("payloadJson", out var pj) ? pj.GetString() : null;

        if (string.IsNullOrWhiteSpace(eventType))
        {
            _logger.LogWarning("Received audit event without eventType from {RoutingKey}.", routingKey);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var queue = scope.ServiceProvider.GetRequiredService<IAuditWriteQueue>();

        var moduleCode = routingKey.Split('.').FirstOrDefault() ?? "unknown";
        var item = new AuditTrailWriteItem(
            messageId ?? Guid.NewGuid(),
            null,
            moduleCode,
            eventType,
            routingKey,
            "consumed",
            DateTimeOffset.UtcNow,
            null,
            innerPayload);

        await queue.EnqueueAsync(item, cancellationToken);

        _logger.AuditEventProcessed(messageId ?? Guid.Empty, eventType, routingKey);
    }
}
