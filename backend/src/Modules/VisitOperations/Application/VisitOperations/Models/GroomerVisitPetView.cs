namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record GroomerVisitPetView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode);