namespace Tailbook.Modules.Booking.Application.Booking.Commands;

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
