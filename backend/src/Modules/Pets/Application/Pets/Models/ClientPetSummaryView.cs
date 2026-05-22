namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record ClientPetSummaryView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode, string? Notes, string? PrimaryPhotoFileName);