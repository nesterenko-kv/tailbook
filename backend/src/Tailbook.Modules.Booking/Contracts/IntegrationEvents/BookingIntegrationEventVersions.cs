using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Contracts.IntegrationEvents;

public static class BookingIntegrationEventVersions
{
    public const int AppointmentCancelled = IntegrationEventVersionPolicy.InitialVersion;
    public const int AppointmentCreated = IntegrationEventVersionPolicy.InitialVersion;
    public const int AppointmentRescheduled = IntegrationEventVersionPolicy.InitialVersion;
    public const int BookingRequestConverted = IntegrationEventVersionPolicy.InitialVersion;
    public const int BookingRequested = IntegrationEventVersionPolicy.InitialVersion;
}
