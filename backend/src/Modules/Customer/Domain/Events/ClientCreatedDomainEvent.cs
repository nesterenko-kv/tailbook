using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Customer.IntegrationEvents;

namespace Tailbook.Modules.Customer.Domain.Events;

public sealed record ClientCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ClientId,
    string DisplayName,
    string Status,
    string? Notes) : IDomainEvent
{
    public string EventType => "ClientCreated";
    public string ModuleCode => "customer";

    public IIntegrationEventDto ToIntegrationEvent() => new ClientCreatedIntegrationEvent(
            ClientId,
            DisplayName,
            Status,
            Notes);
}
