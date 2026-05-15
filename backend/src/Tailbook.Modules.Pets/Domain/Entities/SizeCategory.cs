namespace Tailbook.Modules.Pets.Domain.Entities;

public sealed class SizeCategory
{
    public Guid Id { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? MinWeightKg { get; set; }
    public decimal? MaxWeightKg { get; set; }
}
