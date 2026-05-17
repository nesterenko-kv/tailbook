using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Domain.Events;

public sealed record AppointmentCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AppointmentId,
    Guid? BookingRequestId,
    Guid PetId,
    Guid GroomerId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    int VersionNo) : IDomainEvent
{
    public string EventType => "AppointmentCreated";
    public string ModuleCode => "booking";
}
