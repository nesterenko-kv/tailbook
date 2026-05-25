using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.IntegrationEvents;

public sealed record AppointmentCreatedIntegrationEvent(
    Guid AppointmentId,
    Guid? BookingRequestId,
    Guid PetId,
    Guid GroomerId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    int VersionNo) : IIntegrationEventDto
{
    public int EventVersion => BookingIntegrationEventVersions.AppointmentCreated;
}
