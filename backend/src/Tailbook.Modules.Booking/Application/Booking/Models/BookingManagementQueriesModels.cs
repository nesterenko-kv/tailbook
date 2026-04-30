namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record CreateBookingRequestCommand(
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    string? Channel,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowCommand> PreferredTimes,
    IReadOnlyCollection<CreateBookingRequestItemCommand> Items,
    Guid? PreferredGroomerId = null,
    string? SelectionMode = null,
    GuestBookingIntakeCommand? GuestIntake = null,
    string? InitialStatus = null);

public sealed record AttachBookingRequestContextCommand(
    Guid BookingRequestId,
    Guid? ClientId,
    Guid PetId,
    Guid? RequestedByContactId);

public sealed record GuestBookingIntakeCommand(
    GuestBookingRequesterCommand? Requester,
    GuestBookingPetCommand? Pet);

public sealed record GuestBookingRequesterCommand(
    string? DisplayName,
    string? Phone,
    string? InstagramHandle,
    string? Email,
    string? PreferredContactMethodCode);

public sealed record GuestBookingPetCommand(
    string? DisplayName,
    Guid? AnimalTypeId,
    string? AnimalTypeCode,
    string? AnimalTypeName,
    Guid? BreedId,
    string? BreedCode,
    string? BreedName,
    Guid? CoatTypeId,
    string? CoatTypeCode,
    string? CoatTypeName,
    Guid? SizeCategoryId,
    string? SizeCategoryCode,
    string? SizeCategoryName,
    decimal? WeightKg,
    string? Notes);

public sealed record PreferredTimeWindowCommand(DateTime StartAtUtc, DateTime EndAtUtc, string? Label);
public sealed record CreateBookingRequestItemCommand(Guid OfferId, string? ItemType, string? RequestedNotes);
public sealed record ConvertBookingRequestToAppointmentCommand(Guid BookingRequestId, Guid GroomerId, DateTime StartAtUtc);
public sealed record CreateAppointmentCommand(Guid PetId, Guid GroomerId, DateTime StartAtUtc, IReadOnlyCollection<CreateAppointmentItemCommand> Items);
public sealed record CreateAppointmentItemCommand(Guid OfferId, string? ItemType);
public sealed record RescheduleAppointmentCommand(Guid AppointmentId, Guid GroomerId, DateTime StartAtUtc, int ExpectedVersionNo);
public sealed record CancelAppointmentCommand(Guid AppointmentId, int ExpectedVersionNo, string ReasonCode, string? Notes);

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
public sealed record PreferredTimeWindowView(DateTime StartAtUtc, DateTime EndAtUtc, string? Label);
public sealed record BookingRequestItemView(Guid Id, Guid OfferId, Guid? OfferVersionId, string? ItemType, string? RequestedNotes);
public sealed record BookingRequestListItemView(
    Guid Id,
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    Guid? PreferredGroomerId,
    string? SelectionMode,
    string Channel,
    string Status,
    int ItemCount,
    string? PetDisplayName,
    string? RequesterDisplayName,
    string? RequesterPrimaryContact,
    string? PreferredGroomerName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BookingRequestDetailView(
    Guid Id,
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    Guid? PreferredGroomerId,
    string? PreferredGroomerName,
    string? SelectionMode,
    string Channel,
    string Status,
    BookingRequestSubjectView? Subject,
    IReadOnlyCollection<PreferredTimeWindowView> PreferredTimes,
    string? Notes,
    IReadOnlyCollection<BookingRequestItemView> Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BookingRequestSubjectView(
    string? PetDisplayName,
    string? AnimalTypeCode,
    string? BreedName,
    string? RequesterDisplayName,
    string? RequesterPrimaryContact,
    string? PreferredGroomerName,
    GuestBookingIntakeView? GuestIntake);

public sealed record GuestBookingIntakeView(
    BookingRequesterSnapshotView? Requester,
    BookingGuestPetSnapshotView? Pet);

public sealed record BookingRequesterSnapshotView(
    string? DisplayName,
    string? Phone,
    string? InstagramHandle,
    string? Email,
    string? PreferredContactMethodCode)
{
    public string? PrimaryContactDisplay =>
        !string.IsNullOrWhiteSpace(Phone) ? Phone :
        !string.IsNullOrWhiteSpace(InstagramHandle) ? InstagramHandle :
        !string.IsNullOrWhiteSpace(Email) ? Email : null;
}

public sealed record BookingGuestPetSnapshotView(
    string? DisplayName,
    Guid? AnimalTypeId,
    string? AnimalTypeCode,
    string? AnimalTypeName,
    Guid? BreedId,
    string? BreedCode,
    string? BreedName,
    Guid? CoatTypeId,
    string? CoatTypeCode,
    string? CoatTypeName,
    Guid? SizeCategoryId,
    string? SizeCategoryCode,
    string? SizeCategoryName,
    decimal? WeightKg,
    string? Notes);

public sealed record AppointmentPetView(Guid Id, Guid? ClientId, string AnimalTypeCode, string AnimalTypeName, string BreedName);
public sealed record AppointmentItemView(Guid Id, string ItemType, Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferDisplayName, int Quantity, Guid PriceSnapshotId, Guid DurationSnapshotId, decimal PriceAmount, int ServiceMinutes, int ReservedMinutes);
public sealed record AppointmentListItemView(Guid Id, Guid? BookingRequestId, Guid PetId, Guid GroomerId, DateTime StartAtUtc, DateTime EndAtUtc, string Status, int VersionNo, int ItemCount, decimal TotalAmount);
public sealed record AppointmentDetailView(Guid Id, Guid? BookingRequestId, AppointmentPetView Pet, Guid GroomerId, DateTime StartAtUtc, DateTime EndAtUtc, string Status, int VersionNo, IReadOnlyCollection<AppointmentItemView> Items, decimal TotalAmount, int ServiceMinutes, int ReservedMinutes, string? CancellationReasonCode, string? CancellationNotes, DateTime? CancelledAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
