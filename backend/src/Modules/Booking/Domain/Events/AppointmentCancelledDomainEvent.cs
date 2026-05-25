using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.IntegrationEvents;

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

    public IIntegrationEventDto ToIntegrationEvent() => new AppointmentCancelledIntegrationEvent(AppointmentId, Status, ReasonCode, Notes, VersionNo);
}
