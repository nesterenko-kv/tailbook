namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicPetPayload
{
    public Guid? PetId { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
    public decimal? WeightKg { get; set; }
    public string? PetName { get; set; }
    public string? Notes { get; set; }
}