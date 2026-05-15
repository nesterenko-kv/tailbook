namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateAppointmentInput(Guid PetId, Guid GroomerId, DateTimeOffset StartAt, IReadOnlyCollection<CreateAppointmentItemData> Items);