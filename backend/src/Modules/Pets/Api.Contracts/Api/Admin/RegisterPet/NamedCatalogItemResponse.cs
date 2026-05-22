namespace Tailbook.Modules.Pets.Api.Admin.RegisterPet;

public sealed class NamedCatalogItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}