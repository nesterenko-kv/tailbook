namespace Tailbook.Modules.Pets.Domain;

public sealed class BreedGroup
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
