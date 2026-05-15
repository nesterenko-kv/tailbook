namespace Tailbook.Modules.Pets.Api.Admin.GetPetCatalog;

public sealed class BreedResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<Guid> AllowedCoatTypeIds { get; set; } = [];
    public IReadOnlyCollection<Guid> AllowedSizeCategoryIds { get; set; } = [];
}