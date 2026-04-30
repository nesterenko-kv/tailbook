namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record GroomerAppointmentListItemView(
    Guid Id,
    Guid PetId,
    string PetDisplayName,
    string BreedName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    int ReservedMinutes,
    IReadOnlyCollection<string> ServiceLabels);

public sealed record GroomerAppointmentPetView(
    Guid Id,
    string DisplayName,
    string AnimalTypeCode,
    string AnimalTypeName,
    string BreedName,
    string? CoatTypeCode,
    string? SizeCategoryCode);

public sealed record GroomerAppointmentItemView(
    Guid AppointmentItemId,
    string ItemType,
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCode,
    string OfferDisplayName,
    int Quantity,
    int ServiceMinutes,
    int ReservedMinutes,
    IReadOnlyCollection<string> ExecutionPlanSummary);

public sealed record GroomerAppointmentDetailView(
    Guid Id,
    GroomerAppointmentPetView Pet,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string Status,
    int ReservedMinutes,
    IReadOnlyCollection<string> HandlingNotes,
    IReadOnlyCollection<GroomerAppointmentItemView> Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
