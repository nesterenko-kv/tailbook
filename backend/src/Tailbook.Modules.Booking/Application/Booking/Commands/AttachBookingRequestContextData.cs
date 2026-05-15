namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record AttachBookingRequestContextData(
    Guid BookingRequestId,
    Guid? ClientId,
    Guid PetId,
    Guid? RequestedByContactId);
