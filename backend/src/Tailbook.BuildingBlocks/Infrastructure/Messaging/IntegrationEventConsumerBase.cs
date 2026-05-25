using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

public abstract class IntegrationEventConsumerBase : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger _logger;

    protected IServiceScopeFactory ScopeFactory { get; }

    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }

    protected virtual bool IsConsumerEnabled => true;

    protected IntegrationEventConsumerBase(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _options = rabbitMqOptions.Value;
        ScopeFactory = scopeFactory;
        _logger = logger;
    }

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !IsConsumerEnabled)
        {
            return;
        }

        var exchange = _options.Exchange;
        var channel = await _connectionFactory.CreateChannelAsync(stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        foreach (var routingKey in RoutingKeys)
        {
            await channel.QueueBindAsync(
                queue: QueueName,
                exchange: exchange,
                routingKey: routingKey,
                cancellationToken: stoppingToken
            );
        }

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            using var activity = StartConsumerActivity(exchange, args.RoutingKey);

            try
            {
                await ProcessWithEnvelopeAsync(args.Body, args.RoutingKey, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                RecordConsumerResult(exchange, args.RoutingKey, success: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event from routing key {RoutingKey}.", args.RoutingKey);
                RecordConsumerResult(exchange, args.RoutingKey, success: false);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.ConsumerStarted(QueueName, exchange, RoutingKeys.Length);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.ConsumerStopped(QueueName);
        }
    }

    private async Task ProcessWithEnvelopeAsync(ReadOnlyMemory<byte> body, string routingKey, CancellationToken cancellationToken)
    {
        var payloadJson = Encoding.UTF8.GetString(body.Span);
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
        var messageId = root.TryGetProperty("messageId", out var mid) ? mid.GetGuid() : (Guid?)null;
        var innerPayload = root.TryGetProperty("payloadJson", out var pj) ? pj.GetString() : null;

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(innerPayload))
        {
            _logger.LogWarning("Received malformed event from routing key {RoutingKey}.", routingKey);
            return;
        }

        await ProcessEventAsync(eventType, innerPayload, messageId ?? Guid.NewGuid(), routingKey, cancellationToken);
    }

    protected abstract Task ProcessEventAsync(
        string eventType,
        string innerPayload,
        Guid messageId,
        string routingKey,
        CancellationToken cancellationToken);

    protected virtual Activity? StartConsumerActivity(string exchange, string routingKey) => null;

    protected virtual void RecordConsumerResult(string exchange, string routingKey, bool success)
    {
    }
}
