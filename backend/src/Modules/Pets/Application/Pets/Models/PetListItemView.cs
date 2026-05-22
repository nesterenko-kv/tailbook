namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record PetListItemView(Guid Id, Guid? ClientId, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode, decimal? WeightKg, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);