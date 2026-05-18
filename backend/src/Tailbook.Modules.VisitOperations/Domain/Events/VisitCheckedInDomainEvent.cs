using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.VisitOperations.Contracts.IntegrationEvents;

namespace Tailbook.Modules.VisitOperations.Domain.Events;

public sealed record VisitCheckedInDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    DateTimeOffset CheckedInAt) : IDomainEvent
{
    public string EventType => "VisitCheckedIn";
    public string ModuleCode => "visitops";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new VisitCheckedInIntegrationEvent(VisitId, AppointmentId, Status, CheckedInAt);
    }
}
