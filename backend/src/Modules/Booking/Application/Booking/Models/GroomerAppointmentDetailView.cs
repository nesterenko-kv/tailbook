namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record GroomerAppointmentDetailView(
    Guid Id,
    GroomerAppointmentPetView Pet,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    int ReservedMinutes,
    IReadOnlyCollection<string> HandlingNotes,
    IReadOnlyCollection<GroomerAppointmentItemView> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);