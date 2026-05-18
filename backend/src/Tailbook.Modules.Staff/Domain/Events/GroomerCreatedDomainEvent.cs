using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Staff.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Staff.Domain.Events;

public sealed record GroomerCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid GroomerId,
    Guid? UserId,
    string DisplayName,
    bool Active) : IDomainEvent
{
    public string EventType => "GroomerCreated";
    public string ModuleCode => "staff";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new GroomerCreatedIntegrationEvent(
            GroomerId,
            UserId,
            DisplayName,
            Active);
    }
}
