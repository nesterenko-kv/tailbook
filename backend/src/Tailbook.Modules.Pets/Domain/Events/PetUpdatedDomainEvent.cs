using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Pets.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Pets.Domain.Events;

public sealed record PetUpdatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PetId,
    Guid? ClientId,
    string Name,
    Guid AnimalTypeId,
    Guid BreedId,
    Guid? CoatTypeId,
    Guid? SizeCategoryId,
    DateOnly? BirthDate,
    decimal? WeightKg,
    string? Notes) : IDomainEvent
{
    public string EventType => "PetUpdated";
    public string ModuleCode => "pets";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new PetUpdatedIntegrationEvent(
            PetId,
            Name,
            AnimalTypeId,
            BreedId,
            CoatTypeId,
            SizeCategoryId,
            BirthDate,
            WeightKg,
            Notes);
    }
}
