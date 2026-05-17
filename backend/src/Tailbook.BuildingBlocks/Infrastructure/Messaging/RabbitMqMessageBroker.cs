using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

public sealed class RabbitMqMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMessageBroker> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private bool _disposed;

    public RabbitMqMessageBroker(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqMessageBroker> logger)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string exchange, string routingKey, object payload, CancellationToken cancellationToken = default)
    {
        await PublishAsync(exchange, routingKey, payload, messageId: null, cancellationToken);
    }

    public async Task PublishAsync(string exchange, string routingKey, object payload, string? messageId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(payloadJson);

        using var activity = RabbitMqTelemetry.StartPublishActivity(exchange, routingKey);

        try
        {
            await using var channel = await _connectionFactory.CreateChannelAsync(cancellationToken);

            var exchangeToUse = string.IsNullOrWhiteSpace(exchange) ? _options.Exchange : exchange;

            await channel.ExchangeDeclareAsync(
                exchange: exchangeToUse,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = messageId ?? Guid.NewGuid().ToString("D"),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>
                {
                    ["event_type"] = routingKey
                }
            };

            await channel.BasicPublishAsync(
                exchange: exchangeToUse,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            stopwatch.Stop();
            RabbitMqTelemetry.RecordPublish(exchangeToUse, routingKey, body.Length, stopwatch.Elapsed, success: true);

            _logger.LogDebug(
                "Published message {MessageId} to exchange {Exchange} with routing key {RoutingKey} ({PayloadSize} bytes).",
                props.MessageId, exchangeToUse, routingKey, body.Length);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RabbitMqTelemetry.RecordPublish(exchange, routingKey, body.Length, stopwatch.Elapsed, success: false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(ex,
                "Failed to publish message to exchange {Exchange} with routing key {RoutingKey}.",
                exchange, routingKey);

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await Task.CompletedTask;
    }
}
