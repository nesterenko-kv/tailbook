namespace Tailbook.Modules.Pets.Api.Admin.GetPetCatalog;

public sealed class BreedGroupResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}