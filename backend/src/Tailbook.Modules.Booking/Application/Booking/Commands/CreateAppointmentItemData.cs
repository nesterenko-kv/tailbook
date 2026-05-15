namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateAppointmentItemData(Guid OfferId, string? ItemType);
