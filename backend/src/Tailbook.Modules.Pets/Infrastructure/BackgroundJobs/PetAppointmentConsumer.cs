using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;

namespace Tailbook.Modules.Pets.Infrastructure.BackgroundJobs;

public sealed class PetAppointmentConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly ILogger<PetAppointmentConsumer> _logger;

    private static readonly string[] PetRoutingKeys =
    [
        "booking.appointment-created",
        "booking.appointment-cancelled",
        "booking.appointment-rescheduled"
    ];

    public PetAppointmentConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        ILogger<PetAppointmentConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_rabbitMqOptions.Enabled)
        {
            return;
        }

        var exchange = _rabbitMqOptions.Exchange;
        var queue = "pet-appointments";

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

        foreach (var routingKey in PetRoutingKeys)
        {
            await channel.QueueBindAsync(
                queue: queue,
                exchange: exchange,
                routingKey: routingKey,
                cancellationToken: stoppingToken);
        }

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                await ProcessAppointmentEventAsync(args.Body, args.RoutingKey, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process pet appointment event from routing key {RoutingKey}.",
                    args.RoutingKey);
                await channel.BasicNackAsync(args.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.PetAppointmentConsumerStarted(queue, exchange, PetRoutingKeys.Length);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.PetAppointmentConsumerStopped();
        }
    }

    private async Task ProcessAppointmentEventAsync(ReadOnlyMemory<byte> body, string routingKey, CancellationToken cancellationToken)
    {
        var payloadJson = Encoding.UTF8.GetString(body.Span);
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
        var messageId = root.TryGetProperty("messageId", out var mid) ? mid.GetGuid() : (Guid?)null;
        var innerPayload = root.TryGetProperty("payloadJson", out var pj) ? pj.GetString() : null;

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(innerPayload))
        {
            _logger.LogWarning("Received malformed pet event from {RoutingKey}.", routingKey);
            return;
        }

        using var innerDoc = JsonDocument.Parse(innerPayload);

        _logger.PetAppointmentEventReceived(messageId ?? Guid.Empty, eventType, routingKey);
        await Task.CompletedTask;
    }
}
