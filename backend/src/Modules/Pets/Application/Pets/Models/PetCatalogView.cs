namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record PetCatalogView(IReadOnlyCollection<AnimalTypeView> AnimalTypes, IReadOnlyCollection<BreedGroupView> BreedGroups, IReadOnlyCollection<BreedView> Breeds, IReadOnlyCollection<CoatTypeView> CoatTypes, IReadOnlyCollection<SizeCategoryView> SizeCategories);