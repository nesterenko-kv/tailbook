namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record GroomerAppointmentListItemView(
    Guid Id,
    Guid PetId,
    string PetDisplayName,
    string BreedName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    int ReservedMinutes,
    IReadOnlyCollection<string> ServiceLabels);