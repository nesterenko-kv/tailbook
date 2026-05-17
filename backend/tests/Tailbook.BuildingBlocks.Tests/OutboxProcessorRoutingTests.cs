using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Xunit;

namespace Tailbook.BuildingBlocks.Tests;

public sealed class OutboxProcessorRoutingTests
{
    [Theory]
    [InlineData("booking", "AppointmentCreated", "booking.appointment-created")]
    [InlineData("visitops", "VisitClosed", "visitops.visit-closed")]
    [InlineData("identity", "PasswordResetRequested", "identity.password-reset-requested")]
    [InlineData("booking", "AppointmentRescheduled", "booking.appointment-rescheduled")]
    [InlineData("booking", "BookingRequested", "booking.booking-requested")]
    [InlineData("visitops", "VisitCheckedIn", "visitops.visit-checked-in")]
    [InlineData("visitops", "CompleteVisit", "visitops.complete-visit")]
    public async Task Outbox_message_maps_to_correct_routing_key(string moduleCode, string eventType, string expectedRoutingKey)
    {
        var capturedRoutingKey = string.Empty;
        var broker = new CapturingMessageBroker((_, routingKey, _, _) => capturedRoutingKey = routingKey);

        var dbContext = CreateInMemoryDbContext();
        dbContext.Set<OutboxMessage>().Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            ModuleCode = moduleCode,
            EventType = eventType,
            PayloadJson = """{"test":"data"}""",
            OccurredAt = DateTimeOffset.UtcNow,
            ProcessedAt = null
        });
        await dbContext.SaveChangesAsync();

        await ProcessMessages(dbContext, broker);

        Assert.Equal(expectedRoutingKey, capturedRoutingKey);
        var processedMessage = await dbContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
        Assert.NotNull(processedMessage.ProcessedAt);
    }

    [Fact]
    public async Task Messages_are_ordered_by_occurred_at()
    {
        var captured = new List<(string routingKey, DateTimeOffset? processedAt)>();
        var broker = new CapturingMessageBroker((_, routingKey, _, _) =>
        {
            captured.Add((routingKey, DateTimeOffset.UtcNow));
        });

        var dbContext = CreateInMemoryDbContext();
        var now = DateTimeOffset.UtcNow;
        dbContext.Set<OutboxMessage>().AddRange(
            new OutboxMessage { Id = Guid.NewGuid(), ModuleCode = "b", EventType = "Early", PayloadJson = "{}", OccurredAt = now.AddSeconds(-10), ProcessedAt = null },
            new OutboxMessage { Id = Guid.NewGuid(), ModuleCode = "c", EventType = "Middle", PayloadJson = "{}", OccurredAt = now, ProcessedAt = null },
            new OutboxMessage { Id = Guid.NewGuid(), ModuleCode = "a", EventType = "Late", PayloadJson = "{}", OccurredAt = now.AddSeconds(10), ProcessedAt = null }
        );
        await dbContext.SaveChangesAsync();

        await ProcessMessages(dbContext, broker);

        Assert.Equal(3, captured.Count);
        Assert.Contains("Early", captured[0].routingKey, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Middle", captured[1].routingKey, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Late", captured[2].routingKey, StringComparison.OrdinalIgnoreCase);
    }

    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid():N}")
            .Options;

        var assemblies = new ModelConfigurationAssemblies([]);
        return new AppDbContext(options, assemblies);
    }

    private static async Task ProcessMessages(AppDbContext dbContext, IMessageBroker broker)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(100)
            .ToListAsync();

        foreach (var message in messages)
        {
            var routingKey = ToRoutingKey(message.ModuleCode, message.EventType);
            await broker.PublishAsync("tailbook.events", routingKey, new { message.EventType, message.PayloadJson }, message.Id.ToString("D"));
            message.ProcessedAt = utcNow;
        }

        await dbContext.SaveChangesAsync();
    }

    private static string ToRoutingKey(string moduleCode, string eventType)
    {
        var kebab = string.Concat(
            eventType.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "-" + char.ToLowerInvariant(c)
                    : char.ToLowerInvariant(c).ToString()));
        return $"{moduleCode}.{kebab}";
    }

    private sealed class CapturingMessageBroker(Action<string, string, object, string?> onPublish) : IMessageBroker
    {
        public Task PublishAsync(string exchange, string routingKey, object payload, CancellationToken ct = default)
        {
            onPublish(exchange, routingKey, payload, null);
            return Task.CompletedTask;
        }

        public Task PublishAsync(string exchange, string routingKey, object payload, string? messageId, CancellationToken ct = default)
        {
            onPublish(exchange, routingKey, payload, messageId);
            return Task.CompletedTask;
        }
    }
}
