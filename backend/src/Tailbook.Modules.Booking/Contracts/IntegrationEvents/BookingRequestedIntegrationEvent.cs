using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Contracts.IntegrationEvents;

public sealed record BookingRequestedIntegrationEvent(
    Guid BookingRequestId,
    Guid? PetId,
    Guid? ClientId,
    string Channel,
    string Status,
    string? SelectionMode) : IIntegrationEventDto
{
    public int EventVersion => BookingIntegrationEventVersions.BookingRequested;
}
