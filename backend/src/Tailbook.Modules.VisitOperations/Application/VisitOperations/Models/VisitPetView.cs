namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record VisitPetView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode);