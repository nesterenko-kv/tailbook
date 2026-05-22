namespace Tailbook.Modules.Pets.Api.Admin.GetPetCatalog;

public sealed class AnimalTypeResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}