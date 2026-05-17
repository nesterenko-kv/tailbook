using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Domain.Events;

public sealed record BookingRequestConvertedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookingRequestId,
    Guid AppointmentId) : IDomainEvent
{
    public string EventType => "BookingRequestConverted";
    public string ModuleCode => "booking";
}
