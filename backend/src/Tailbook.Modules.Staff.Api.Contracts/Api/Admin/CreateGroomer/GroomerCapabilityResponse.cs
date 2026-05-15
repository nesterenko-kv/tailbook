namespace Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

public sealed class GroomerCapabilityResponse
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
    public DateTimeOffset CreatedAt { get; set; }
}