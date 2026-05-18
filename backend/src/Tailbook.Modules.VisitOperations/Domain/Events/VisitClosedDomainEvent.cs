using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.VisitOperations.Contracts.IntegrationEvents;

namespace Tailbook.Modules.VisitOperations.Domain.Events;

public sealed record VisitClosedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    decimal FinalTotalAmount,
    DateTimeOffset ClosedAt) : IDomainEvent
{
    public string EventType => "VisitClosed";
    public string ModuleCode => "visitops";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new VisitClosedIntegrationEvent(VisitId, AppointmentId, Status, FinalTotalAmount, ClosedAt);
    }
}
