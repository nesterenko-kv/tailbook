namespace Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog;

public sealed class PublicBreedGroupResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}