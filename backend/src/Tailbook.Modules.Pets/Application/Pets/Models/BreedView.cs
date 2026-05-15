namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record BreedView(Guid Id, Guid AnimalTypeId, Guid? BreedGroupId, string Code, string Name, IReadOnlyCollection<Guid> AllowedCoatTypeIds, IReadOnlyCollection<Guid> AllowedSizeCategoryIds);