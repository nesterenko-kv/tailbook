namespace Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog;

public sealed class GetPublicPetCatalogResponse
{
    public IReadOnlyCollection<PublicAnimalTypeResponse> AnimalTypes { get; set; } = [];
    public IReadOnlyCollection<PublicBreedGroupResponse> BreedGroups { get; set; } = [];
    public IReadOnlyCollection<PublicBreedResponse> Breeds { get; set; } = [];
    public IReadOnlyCollection<PublicCoatTypeResponse> CoatTypes { get; set; } = [];
    public IReadOnlyCollection<PublicSizeCategoryResponse> SizeCategories { get; set; } = [];
}