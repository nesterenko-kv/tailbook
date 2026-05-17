using Tailbook.BuildingBlocks.Abstractions;

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
}
