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

public abstract class IntegrationEventConsumerBase(
    RabbitMqConnectionFactory connectionFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IServiceScopeFactory scopeFactory,
    ILogger logger
)
    : BackgroundService
{
    private readonly RabbitMqOptions _options = rabbitMqOptions.Value;

    protected IServiceScopeFactory ScopeFactory { get; } = scopeFactory;

    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }

    protected virtual bool IsConsumerEnabled => true;

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !IsConsumerEnabled) return;

        string exchange = _options.Exchange;
        IChannel channel = await connectionFactory.CreateChannelAsync(stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange,
            ExchangeType.Topic,
            true,
            false,
            cancellationToken: stoppingToken
        );

        await channel.QueueDeclareAsync(
            QueueName,
            true,
            false,
            false,
            cancellationToken: stoppingToken
        );

        foreach (string routingKey in RoutingKeys)
            await channel.QueueBindAsync(
                QueueName,
                exchange,
                routingKey,
                cancellationToken: stoppingToken
            );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            using Activity? activity = StartConsumerActivity(exchange, args.RoutingKey);

            try
            {
                await ProcessWithEnvelopeAsync(args.Body, args.RoutingKey, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
                RecordConsumerResult(exchange, args.RoutingKey, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process event from routing key {RoutingKey}.", args.RoutingKey);
                RecordConsumerResult(exchange, args.RoutingKey, false);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                await channel.BasicNackAsync(args.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, false, consumer, stoppingToken);

        logger.ConsumerStarted(QueueName, exchange, RoutingKeys.Length);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.ConsumerStopped(QueueName);
        }
    }

    private async Task ProcessWithEnvelopeAsync(ReadOnlyMemory<byte> body, string routingKey,
        CancellationToken cancellationToken)
    {
        string payloadJson = Encoding.UTF8.GetString(body.Span);
        using var document = JsonDocument.Parse(payloadJson);
        JsonElement root = document.RootElement;

        string? eventType = root.TryGetProperty("eventType", out JsonElement et) ? et.GetString() : null;
        Guid? messageId = root.TryGetProperty("messageId", out JsonElement mid) ? mid.GetGuid() : null;
        string? innerPayload = root.TryGetProperty("payloadJson", out JsonElement pj) ? pj.GetString() : null;

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(innerPayload))
        {
            logger.LogWarning("Received malformed event from routing key {RoutingKey}.", routingKey);
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

    protected virtual Activity? StartConsumerActivity(string exchange, string routingKey)
    {
        return null;
    }

    protected virtual void RecordConsumerResult(string exchange, string routingKey, bool success)
    {
    }
}
