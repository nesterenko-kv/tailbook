namespace Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog;

public sealed class PublicBreedResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<Guid> AllowedCoatTypeIds { get; set; } = [];
    public IReadOnlyCollection<Guid> AllowedSizeCategoryIds { get; set; } = [];
}