namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record SizeCategoryView(Guid Id, Guid? AnimalTypeId, string Code, string Name, decimal? MinWeightKg, decimal? MaxWeightKg);