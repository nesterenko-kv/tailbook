using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Contracts.IntegrationEvents;

public sealed record AppointmentRescheduledIntegrationEvent(
    Guid AppointmentId,
    Guid GroomerId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    int VersionNo) : IIntegrationEventDto
{
    public int EventVersion => BookingIntegrationEventVersions.AppointmentRescheduled;
}
