using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Pets.Contracts.IntegrationEvents;

public sealed record PetUpdatedIntegrationEvent(
    Guid PetId,
    string Name,
    Guid AnimalTypeId,
    Guid BreedId,
    Guid? CoatTypeId,
    Guid? SizeCategoryId,
    DateOnly? BirthDate,
    decimal? WeightKg,
    string? Notes) : IIntegrationEventDto
{
    public int EventVersion => PetsIntegrationEventVersions.PetUpdated;
}
