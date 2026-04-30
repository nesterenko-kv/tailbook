namespace Tailbook.Modules.Pets.Domain.Aggregates;

public sealed class Pet
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid AnimalTypeId { get; set; }
    public Guid BreedId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
