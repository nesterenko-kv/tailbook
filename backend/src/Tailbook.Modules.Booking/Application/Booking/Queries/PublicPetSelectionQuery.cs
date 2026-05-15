namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PublicPetSelectionQuery(
    Guid? PetId,
    Guid? AnimalTypeId,
    Guid? BreedId,
    Guid? CoatTypeId,
    Guid? SizeCategoryId,
    decimal? WeightKg,
    string? PetName,
    string? Notes);