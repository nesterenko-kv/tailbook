namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record AppointmentListItemView(Guid Id, Guid? BookingRequestId, Guid PetId, Guid GroomerId, DateTimeOffset StartAt, DateTimeOffset EndAt, string Status, int VersionNo, int ItemCount, decimal TotalAmount);