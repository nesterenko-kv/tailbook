namespace Tailbook.Modules.Staff.Application.Staff.Models;

public sealed record GroomerCapabilityView(Guid Id, Guid GroomerId, Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId, Guid? OfferId, string CapabilityMode, int ReservedDurationModifierMinutes, string? Notes, DateTimeOffset CreatedAt);