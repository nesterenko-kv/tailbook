namespace Tailbook.Modules.Staff.Domain;

public sealed class GroomerCapability
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
    public Guid? OfferId { get; set; }
    public string CapabilityMode { get; set; } = string.Empty;
    public int ReservedDurationModifierMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
