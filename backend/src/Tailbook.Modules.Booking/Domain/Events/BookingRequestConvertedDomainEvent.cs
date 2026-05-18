using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Booking.Domain.Events;

public sealed record BookingRequestConvertedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookingRequestId,
    Guid AppointmentId) : IDomainEvent
{
    public string EventType => "BookingRequestConverted";
    public string ModuleCode => "booking";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new BookingRequestConvertedIntegrationEvent(BookingRequestId, AppointmentId);
    }
}
