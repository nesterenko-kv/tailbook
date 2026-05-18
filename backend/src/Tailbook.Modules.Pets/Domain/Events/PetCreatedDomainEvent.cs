using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Pets.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Pets.Domain.Events;

public sealed record PetCreatedDomainEvent(
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
    public string EventType => "PetCreated";
    public string ModuleCode => "pets";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new PetCreatedIntegrationEvent(
            PetId,
            ClientId,
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
