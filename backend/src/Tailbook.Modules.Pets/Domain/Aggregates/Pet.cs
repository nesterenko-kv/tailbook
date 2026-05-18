using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Pets.Domain.Events;

namespace Tailbook.Modules.Pets.Domain.Aggregates;

public sealed class Pet : AggregateRoot
{
    private Pet()
    {
    }

    public Guid? ClientId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid AnimalTypeId { get; private set; }
    public Guid BreedId { get; private set; }
    public Guid? CoatTypeId { get; private set; }
    public Guid? SizeCategoryId { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public decimal? WeightKg { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Pet Create(
        Guid? clientId,
        string name,
        Guid animalTypeId,
        Guid breedId,
        Guid? coatTypeId,
        Guid? sizeCategoryId,
        DateOnly? birthDate,
        decimal? weightKg,
        string? notes,
        DateTimeOffset utcNow)
    {
        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Name = name.Trim(),
            AnimalTypeId = animalTypeId,
            BreedId = breedId,
            CoatTypeId = coatTypeId,
            SizeCategoryId = sizeCategoryId,
            BirthDate = birthDate,
            WeightKg = weightKg,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = utcNow.ToUniversalTime(),
            UpdatedAt = utcNow.ToUniversalTime()
        };

        pet.RaiseDomainEvent(new PetCreatedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            pet.Id,
            pet.ClientId,
            pet.Name,
            pet.AnimalTypeId,
            pet.BreedId,
            pet.CoatTypeId,
            pet.SizeCategoryId,
            pet.BirthDate,
            pet.WeightKg,
            pet.Notes));

        return pet;
    }

    public void Update(
        string name,
        Guid animalTypeId,
        Guid breedId,
        Guid? coatTypeId,
        Guid? sizeCategoryId,
        DateOnly? birthDate,
        decimal? weightKg,
        string? notes,
        DateTimeOffset utcNow)
    {
        Name = name.Trim();
        AnimalTypeId = animalTypeId;
        BreedId = breedId;
        CoatTypeId = coatTypeId;
        SizeCategoryId = sizeCategoryId;
        BirthDate = birthDate;
        WeightKg = weightKg;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAt = utcNow.ToUniversalTime();

        RaiseDomainEvent(new PetUpdatedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            Id,
            ClientId,
            Name,
            AnimalTypeId,
            BreedId,
            CoatTypeId,
            SizeCategoryId,
            BirthDate,
            WeightKg,
            Notes));
    }
}
