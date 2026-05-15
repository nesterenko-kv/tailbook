namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record GuestBookingPetInput(
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