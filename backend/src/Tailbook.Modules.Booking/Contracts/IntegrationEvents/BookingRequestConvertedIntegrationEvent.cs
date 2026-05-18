using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Contracts.IntegrationEvents;

public sealed record BookingRequestConvertedIntegrationEvent(
    Guid BookingRequestId,
    Guid AppointmentId) : IIntegrationEventDto
{
    public int EventVersion => BookingIntegrationEventVersions.BookingRequestConverted;
}
