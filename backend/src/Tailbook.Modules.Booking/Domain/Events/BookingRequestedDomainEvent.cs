using Tailbook.BuildingBlocks.Abstractions;

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
}
