using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations.Domain.Aggregates;
using static Tailbook.Modules.VisitOperations.Contracts.VisitStatusCodes;

namespace Tailbook.Modules.VisitOperations.Infrastructure.BackgroundJobs;

public sealed class VisitCancellationConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VisitCancellationConsumer> _logger;

    private const string AppointmentCancelledRoutingKey = "booking.appointment-cancelled";

    public VisitCancellationConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<VisitCancellationConsumer> logger)
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
        var queue = "visitops-cancellations";

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
            routingKey: AppointmentCancelledRoutingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                await ProcessCancellationAsync(args.Body, args.RoutingKey, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process appointment cancellation from routing key {RoutingKey}.",
                    args.RoutingKey);
                await channel.BasicNackAsync(args.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.VisitCancellationConsumerStarted(queue, exchange, AppointmentCancelledRoutingKey);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.VisitCancellationConsumerStopped();
        }
    }

    private async Task ProcessCancellationAsync(ReadOnlyMemory<byte> body, string routingKey, CancellationToken cancellationToken)
    {
        var payloadJson = Encoding.UTF8.GetString(body.Span);
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
        var messageId = root.TryGetProperty("messageId", out var mid) ? mid.GetGuid() : (Guid?)null;
        var innerPayload = root.TryGetProperty("payloadJson", out var pj) ? pj.GetString() : null;

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(innerPayload))
        {
            _logger.LogWarning("Received malformed cancellation event from {RoutingKey}.", routingKey);
            return;
        }

        using var payloadDoc = JsonDocument.Parse(innerPayload);
        var payload = payloadDoc.RootElement;

        var appointmentId = payload.TryGetProperty("appointmentId", out var aid) ? aid.GetGuid() : (Guid?)null;
        if (appointmentId is null || appointmentId == Guid.Empty)
        {
            _logger.LogWarning("Cancellation event missing appointmentId from {RoutingKey}.", routingKey);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var visit = await dbContext.Set<Visit>()
            .Where(v => v.AppointmentId == appointmentId.Value)
            .Where(v => v.Status == Open || v.Status == InProgress)
            .FirstOrDefaultAsync(cancellationToken);

        if (visit is null)
        {
            _logger.AppointmentCancellationNoVisit(messageId ?? Guid.Empty, appointmentId.Value);
            return;
        }

        _logger.AppointmentCancellationProcessing(messageId ?? Guid.Empty, appointmentId.Value, visit.Id, visit.Status);
    }
}
