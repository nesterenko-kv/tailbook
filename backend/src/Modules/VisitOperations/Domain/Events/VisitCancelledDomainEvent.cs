using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.VisitOperations.IntegrationEvents;

namespace Tailbook.Modules.VisitOperations.Domain.Events;

public sealed record VisitCancelledDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    string? ReasonCode) : IDomainEvent
{
    public string EventType => "VisitCancelled";
    public string ModuleCode => "visitops";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new VisitCancelledIntegrationEvent(VisitId, AppointmentId, Status, ReasonCode);
    }
}
