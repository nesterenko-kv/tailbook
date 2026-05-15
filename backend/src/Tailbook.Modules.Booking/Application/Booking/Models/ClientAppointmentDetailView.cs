namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record ClientAppointmentDetailView(
    Guid Id,
    Guid? BookingRequestId,
    Guid PetId,
    string BreedName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Status,
    IReadOnlyCollection<ClientAppointmentItemView> Items,
    decimal TotalAmount,
    int ServiceMinutes,
    int ReservedMinutes,
    string? CancellationReasonCode,
    string? CancellationNotes,
    DateTimeOffset? CancelledAt);