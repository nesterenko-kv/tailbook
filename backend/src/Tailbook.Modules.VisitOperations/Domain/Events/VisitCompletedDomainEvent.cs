using Tailbook.BuildingBlocks.Abstractions;

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
}
