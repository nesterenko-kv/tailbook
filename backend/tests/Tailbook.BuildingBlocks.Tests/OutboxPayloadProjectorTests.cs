using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Xunit;

namespace Tailbook.BuildingBlocks.Tests;

public sealed class OutboxPayloadProjectorTests
{
    [Fact]
    public async Task Interceptor_stages_explicit_contract_payload_with_event_version()
    {
        var interceptor = new DomainEventToOutboxInterceptor();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new TestDbContext(options);
        var domainEvent = new SampleDomainEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Scheduled",
            3);
        var aggregate = new TestAggregate();
        aggregate.Raise(domainEvent);

        dbContext.Aggregates.Add(aggregate);
        await dbContext.SaveChangesAsync();

        var message = await dbContext.Set<OutboxMessage>().SingleAsync();
        using var document = JsonDocument.Parse(message.PayloadJson);

        Assert.Equal(domainEvent.EventId, message.Id);
        Assert.Equal(domainEvent.EventType, message.EventType);
        Assert.Equal(domainEvent.ModuleCode, message.ModuleCode);
        Assert.Equal(domainEvent.OccurredAt, message.OccurredAt);

        Assert.False(document.RootElement.TryGetProperty("eventId", out _));
        Assert.False(document.RootElement.TryGetProperty("occurredAt", out _));
        Assert.False(document.RootElement.TryGetProperty("eventType", out _));
        Assert.False(document.RootElement.TryGetProperty("moduleCode", out _));

        Assert.Equal(1, document.RootElement.GetProperty("eventVersion").GetInt32());
        Assert.Equal(domainEvent.AppointmentId, document.RootElement.GetProperty("appointmentId").GetGuid());
        Assert.Equal(domainEvent.Status, document.RootElement.GetProperty("status").GetString());
        Assert.Equal(domainEvent.VersionNo, document.RootElement.GetProperty("versionNo").GetInt32());
    }

    [Fact]
    public async Task Interceptor_rejects_integration_contract_without_valid_event_version()
    {
        var interceptor = new DomainEventToOutboxInterceptor();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new TestDbContext(options);
        var aggregate = new TestAggregate();
        aggregate.Raise(new InvalidVersionDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow));

        dbContext.Aggregates.Add(aggregate);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
        Assert.Contains("eventVersion", exception.Message, StringComparison.Ordinal);
    }

    private sealed record SampleDomainEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        Guid AppointmentId,
        string Status,
        int VersionNo) : IDomainEvent
    {
        public string EventType => "SampleCreated";
        public string ModuleCode => "sample";

        public IIntegrationEventDto ToIntegrationEvent()
        {
            return new SampleIntegrationEvent(AppointmentId, Status, VersionNo);
        }
    }

    private sealed record SampleIntegrationEvent(
        Guid AppointmentId,
        string Status,
        int VersionNo) : IIntegrationEventDto
    {
        public int EventVersion => IntegrationEventVersionPolicy.InitialVersion;
    }

    private sealed record InvalidVersionDomainEvent(
        Guid EventId,
        DateTimeOffset OccurredAt) : IDomainEvent
    {
        public string EventType => "InvalidVersion";
        public string ModuleCode => "sample";

        public IIntegrationEventDto ToIntegrationEvent()
        {
            return new InvalidVersionIntegrationEvent();
        }
    }

    private sealed record InvalidVersionIntegrationEvent : IIntegrationEventDto
    {
        public int EventVersion => 0;
    }

    private sealed class TestAggregate : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _events = [];
        public Guid Id { get; set; } = Guid.NewGuid();

        public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _events.AsReadOnly();

        public void ClearDomainEvents() => _events.Clear();

        public void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>().HasKey(x => x.Id);
            modelBuilder.Entity<OutboxMessage>().HasKey(x => x.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}
