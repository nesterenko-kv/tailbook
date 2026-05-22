using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Staff.Infrastructure.Services;

namespace Tailbook.Modules.Staff.Infrastructure.BackgroundJobs;

public sealed class StaffAppointmentConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaffAppointmentConsumer> _logger;

    private static readonly string[] AppointmentRoutingKeys =
    [
        "booking.appointment-created",
        "booking.appointment-cancelled",
        "booking.appointment-rescheduled"
    ];

    public StaffAppointmentConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<StaffAppointmentConsumer> logger)
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
        var queue = "staff-appointments";

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

        foreach (var routingKey in AppointmentRoutingKeys)
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
                    "Failed to process appointment event from routing key {RoutingKey}.",
                    args.RoutingKey);
                await channel.BasicNackAsync(args.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.StaffAppointmentConsumerStarted(queue, exchange, AppointmentRoutingKeys.Length);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.StaffAppointmentConsumerStopped();
        }
    }

    private async Task ProcessAppointmentEventAsync(ReadOnlyMemory<byte> body, string routingKey, CancellationToken cancellationToken)
    {
        var payloadJson = Encoding.UTF8.GetString(body.Span);
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
        var messageId = root.TryGetProperty("messageId", out var mid) ? mid.GetGuid() : (Guid?)null;

        if (string.IsNullOrWhiteSpace(eventType))
        {
            _logger.LogWarning("Received appointment event without eventType from {RoutingKey}.", routingKey);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var staffUseCases = scope.ServiceProvider.GetRequiredService<StaffUseCases>();

        _logger.AppointmentEventReceived(messageId ?? Guid.Empty, eventType, routingKey);
        await Task.CompletedTask;
    }
}
