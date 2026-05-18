using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Booking.Domain.Events;

public sealed record BookingRequestedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookingRequestId,
    Guid? PetId,
    Guid? ClientId,
    string Channel,
    string Status,
    string? SelectionMode) : IDomainEvent
{
    public string EventType => "BookingRequested";
    public string ModuleCode => "booking";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new BookingRequestedIntegrationEvent(BookingRequestId, PetId, ClientId, Channel, Status, SelectionMode);
    }
}
