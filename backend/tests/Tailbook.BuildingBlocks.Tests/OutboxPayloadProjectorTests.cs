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
    public async Task Interceptor_stages_business_payload_without_domain_event_metadata()
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

        Assert.Equal(domainEvent.AppointmentId, document.RootElement.GetProperty("appointmentId").GetGuid());
        Assert.Equal(domainEvent.Status, document.RootElement.GetProperty("status").GetString());
        Assert.Equal(domainEvent.VersionNo, document.RootElement.GetProperty("versionNo").GetInt32());
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
