using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Contracts.IntegrationEvents;

public sealed record AppointmentCancelledIntegrationEvent(
    Guid AppointmentId,
    string Status,
    string? ReasonCode,
    string? Notes,
    int VersionNo) : IIntegrationEventDto
{
    public int EventVersion => BookingIntegrationEventVersions.AppointmentCancelled;
}
