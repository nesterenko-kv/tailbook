namespace Tailbook.Modules.Pets.Api.Admin.GetPetCatalog;

public sealed class GetPetCatalogResponse
{
    public IReadOnlyCollection<AnimalTypeResponse> AnimalTypes { get; set; } = [];
    public IReadOnlyCollection<BreedGroupResponse> BreedGroups { get; set; } = [];
    public IReadOnlyCollection<BreedResponse> Breeds { get; set; } = [];
    public IReadOnlyCollection<CoatTypeResponse> CoatTypes { get; set; } = [];
    public IReadOnlyCollection<SizeCategoryResponse> SizeCategories { get; set; } = [];
}