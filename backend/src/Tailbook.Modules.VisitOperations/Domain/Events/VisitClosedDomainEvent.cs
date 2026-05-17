using Tailbook.BuildingBlocks.Abstractions;

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
}
