namespace Tailbook.Modules.Pets.Api.Admin.UpdatePet;

public sealed class UpdatePetRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AnimalTypeCode { get; set; } = string.Empty;
    public Guid BreedId { get; set; }
    public string? CoatTypeCode { get; set; }
    public string? SizeCategoryCode { get; set; }
    public DateOnly? BirthDate { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Notes { get; set; }
}