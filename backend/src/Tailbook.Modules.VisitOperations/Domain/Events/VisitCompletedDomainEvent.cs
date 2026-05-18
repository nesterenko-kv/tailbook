using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.VisitOperations.Contracts.IntegrationEvents;

namespace Tailbook.Modules.VisitOperations.Domain.Events;

public sealed record VisitCompletedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    DateTimeOffset CompletedAt) : IDomainEvent
{
    public string EventType => "VisitCompleted";
    public string ModuleCode => "visitops";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new VisitCompletedIntegrationEvent(VisitId, AppointmentId, Status, CompletedAt);
    }
}
