using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Domain.Events;

public sealed record AppointmentCancelledDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AppointmentId,
    string Status,
    string? ReasonCode,
    string? Notes,
    int VersionNo) : IDomainEvent
{
    public string EventType => "AppointmentCancelled";
    public string ModuleCode => "booking";
}
