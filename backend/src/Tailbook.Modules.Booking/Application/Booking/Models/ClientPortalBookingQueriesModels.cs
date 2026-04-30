namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record CreateClientBookingRequestCommand(
    Guid PetId,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowCommand> PreferredTimes,
    IReadOnlyCollection<CreateClientBookingRequestItemCommand> Items);

public sealed record CreateClientBookingRequestItemCommand(Guid OfferId, string? ItemType, string? RequestedNotes);

public sealed record ClientBookableOfferView(
    Guid Id,
    string OfferType,
    string DisplayName,
    string Currency,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);

public sealed record ClientAppointmentSummaryView(
    Guid Id,
    Guid PetId,
    string PetName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    IReadOnlyCollection<string> ItemLabels);

public sealed record ClientAppointmentItemView(
    Guid Id,
    string ItemType,
    string OfferDisplayName,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);

public sealed record ClientAppointmentDetailView(
    Guid Id,
    Guid? BookingRequestId,
    Guid PetId,
    string BreedName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    IReadOnlyCollection<ClientAppointmentItemView> Items,
    decimal TotalAmount,
    int ServiceMinutes,
    int ReservedMinutes,
    string? CancellationReasonCode,
    string? CancellationNotes,
    DateTime? CancelledAtUtc);
