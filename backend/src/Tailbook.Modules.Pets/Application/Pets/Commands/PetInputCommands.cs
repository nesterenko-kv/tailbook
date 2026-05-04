namespace Tailbook.Modules.Pets.Application.Pets.Commands;

public sealed record RegisterPetCommand(Guid? ClientId, string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
public sealed record UpdatePetCommand(string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
