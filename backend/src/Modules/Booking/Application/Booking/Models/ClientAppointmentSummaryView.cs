namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record ClientAppointmentSummaryView(
    Guid Id,
    Guid PetId,
    string PetName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    IReadOnlyCollection<string> ItemLabels);