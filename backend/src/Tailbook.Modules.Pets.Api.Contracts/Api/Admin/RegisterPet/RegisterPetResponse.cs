namespace Tailbook.Modules.Pets.Api.Admin.RegisterPet;

public sealed class RegisterPetResponse
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public NamedCatalogItemResponse AnimalType { get; set; } = new();
    public BreedResponse Breed { get; set; } = new();
    public NamedCatalogItemResponse? CoatType { get; set; }
    public SizeCategoryItemResponse? SizeCategory { get; set; }
    public DateOnly? BirthDate { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}