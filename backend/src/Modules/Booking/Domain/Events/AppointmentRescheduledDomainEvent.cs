using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.IntegrationEvents;

namespace Tailbook.Modules.Booking.Domain.Events;

public sealed record AppointmentRescheduledDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AppointmentId,
    Guid GroomerId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    int VersionNo) : IDomainEvent
{
    public string EventType => "AppointmentRescheduled";
    public string ModuleCode => "booking";

    public IIntegrationEventDto ToIntegrationEvent() => new AppointmentRescheduledIntegrationEvent(AppointmentId, GroomerId, StartAt, EndAt, Status, VersionNo);
}
