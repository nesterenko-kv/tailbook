using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Contracts.IntegrationEvents;

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

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new AppointmentCreatedIntegrationEvent(
            AppointmentId,
            BookingRequestId,
            PetId,
            GroomerId,
            StartAt,
            EndAt,
            Status,
            VersionNo);
    }
}
