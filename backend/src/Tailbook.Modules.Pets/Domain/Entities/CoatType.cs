namespace Tailbook.Modules.Pets.Domain.Entities;

public sealed class CoatType
{
    public Guid Id { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
