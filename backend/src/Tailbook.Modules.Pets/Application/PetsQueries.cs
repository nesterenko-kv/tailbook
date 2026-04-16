using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Pets.Domain;

namespace Tailbook.Modules.Pets.Application;

public sealed class PetsQueries(
    AppDbContext dbContext,
    IClientReferenceValidationService clientReferenceValidationService,
    IPetContactReadModelService petContactReadModelService,
    IAccessAuditService accessAuditService)
{
    public async Task<PetCatalogView> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var animalTypes = await dbContext.Set<AnimalType>().OrderBy(x => x.Name).Select(x => new AnimalTypeView(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);
        var breedGroups = await dbContext.Set<BreedGroup>().OrderBy(x => x.Name).Select(x => new BreedGroupView(x.Id, x.AnimalTypeId, x.Code, x.Name)).ToListAsync(cancellationToken);
        var breeds = await dbContext.Set<Breed>().OrderBy(x => x.Name).Select(x => new BreedView(x.Id, x.AnimalTypeId, x.BreedGroupId, x.Code, x.Name)).ToListAsync(cancellationToken);
        var coatTypes = await dbContext.Set<CoatType>().OrderBy(x => x.Name).Select(x => new CoatTypeView(x.Id, x.AnimalTypeId, x.Code, x.Name)).ToListAsync(cancellationToken);
        var sizeCategories = await dbContext.Set<SizeCategory>().OrderBy(x => x.Name).Select(x => new SizeCategoryView(x.Id, x.AnimalTypeId, x.Code, x.Name, x.MinWeightKg, x.MaxWeightKg)).ToListAsync(cancellationToken);
        return new PetCatalogView(animalTypes, breedGroups, breeds, coatTypes, sizeCategories);
    }

    public async Task<PetDetailView?> GetPetAsync(Guid petId, Guid? actorUserId, bool includeContacts, CancellationToken cancellationToken)
    {
        var pet = await LoadPetProjectionAsync(petId, cancellationToken);
        if (pet is null)
        {
            return null;
        }

        var contacts = includeContacts ? await petContactReadModelService.GetPetContactsAsync(petId, cancellationToken) : [];
        if (includeContacts && actorUserId.HasValue)
        {
            await accessAuditService.RecordAsync("pet_contact_links", petId.ToString("D"), "READ_CONTACT_DATA", actorUserId, cancellationToken);
        }

        return pet with { Contacts = contacts };
    }

    public async Task<PetDetailView> RegisterPetAsync(RegisterPetCommand command, CancellationToken cancellationToken)
    {
        if (command.ClientId is not null)
        {
            var clientExists = await clientReferenceValidationService.ExistsAsync(command.ClientId.Value, cancellationToken);
            if (!clientExists)
            {
                throw new InvalidOperationException("Client does not exist.");
            }
        }

        var animalType = await dbContext.Set<AnimalType>().SingleOrDefaultAsync(x => x.Code == command.AnimalTypeCode, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown animal type '{command.AnimalTypeCode}'.");
        var breed = await dbContext.Set<Breed>().SingleOrDefaultAsync(x => x.Id == command.BreedId, cancellationToken)
            ?? throw new InvalidOperationException("Breed does not exist.");
        if (breed.AnimalTypeId != animalType.Id)
        {
            throw new InvalidOperationException("Breed must belong to the selected animal type.");
        }

        CoatType? coatType = null;
        if (!string.IsNullOrWhiteSpace(command.CoatTypeCode))
        {
            coatType = await dbContext.Set<CoatType>().SingleOrDefaultAsync(x => x.Code == command.CoatTypeCode, cancellationToken)
                ?? throw new InvalidOperationException($"Unknown coat type '{command.CoatTypeCode}'.");
            if (coatType.AnimalTypeId is not null && coatType.AnimalTypeId != animalType.Id)
            {
                throw new InvalidOperationException("Coat type must belong to the selected animal type when scoped by animal type.");
            }
        }

        SizeCategory? sizeCategory = null;
        if (!string.IsNullOrWhiteSpace(command.SizeCategoryCode))
        {
            sizeCategory = await dbContext.Set<SizeCategory>().SingleOrDefaultAsync(x => x.Code == command.SizeCategoryCode, cancellationToken)
                ?? throw new InvalidOperationException($"Unknown size category '{command.SizeCategoryCode}'.");
            if (sizeCategory.AnimalTypeId is not null && sizeCategory.AnimalTypeId != animalType.Id)
            {
                throw new InvalidOperationException("Size category must belong to the selected animal type when scoped by animal type.");
            }
        }

        var utcNow = DateTime.UtcNow;
        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            ClientId = command.ClientId,
            Name = command.Name.Trim(),
            AnimalTypeId = animalType.Id,
            BreedId = breed.Id,
            CoatTypeId = coatType?.Id,
            SizeCategoryId = sizeCategory?.Id,
            BirthDate = command.BirthDate,
            WeightKg = command.WeightKg,
            Notes = NormalizeOptional(command.Notes),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<Pet>().Add(pet);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadPetProjectionAsync(pet.Id, cancellationToken))!;
    }

    public async Task<PetDetailView?> UpdatePetAsync(Guid petId, UpdatePetCommand command, CancellationToken cancellationToken)
    {
        var pet = await dbContext.Set<Pet>().SingleOrDefaultAsync(x => x.Id == petId, cancellationToken);
        if (pet is null)
        {
            return null;
        }

        var animalType = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == command.AnimalTypeCode, cancellationToken);
        var breed = await dbContext.Set<Breed>().SingleAsync(x => x.Id == command.BreedId, cancellationToken);
        if (breed.AnimalTypeId != animalType.Id)
        {
            throw new InvalidOperationException("Breed must belong to the selected animal type.");
        }

        CoatType? coatType = null;
        if (!string.IsNullOrWhiteSpace(command.CoatTypeCode))
        {
            coatType = await dbContext.Set<CoatType>().SingleAsync(x => x.Code == command.CoatTypeCode, cancellationToken);
            if (coatType.AnimalTypeId is not null && coatType.AnimalTypeId != animalType.Id)
            {
                throw new InvalidOperationException("Coat type must belong to the selected animal type when scoped by animal type.");
            }
        }

        SizeCategory? sizeCategory = null;
        if (!string.IsNullOrWhiteSpace(command.SizeCategoryCode))
        {
            sizeCategory = await dbContext.Set<SizeCategory>().SingleAsync(x => x.Code == command.SizeCategoryCode, cancellationToken);
            if (sizeCategory.AnimalTypeId is not null && sizeCategory.AnimalTypeId != animalType.Id)
            {
                throw new InvalidOperationException("Size category must belong to the selected animal type when scoped by animal type.");
            }
        }

        pet.Name = command.Name.Trim();
        pet.AnimalTypeId = animalType.Id;
        pet.BreedId = breed.Id;
        pet.CoatTypeId = coatType?.Id;
        pet.SizeCategoryId = sizeCategory?.Id;
        pet.BirthDate = command.BirthDate;
        pet.WeightKg = command.WeightKg;
        pet.Notes = NormalizeOptional(command.Notes);
        pet.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadPetProjectionAsync(pet.Id, cancellationToken);
    }

    private async Task<PetDetailView?> LoadPetProjectionAsync(Guid petId, CancellationToken cancellationToken)
    {
        var pet = await dbContext.Set<Pet>()
            .Where(x => x.Id == petId)
            .Join(dbContext.Set<AnimalType>(), x => x.AnimalTypeId, y => y.Id, (x, y) => new { Pet = x, AnimalType = y })
            .Join(dbContext.Set<Breed>(), x => x.Pet.BreedId, y => y.Id, (x, y) => new { x.Pet, x.AnimalType, Breed = y })
            .SingleOrDefaultAsync(cancellationToken);

        if (pet is null)
        {
            return null;
        }

        var coatType = pet.Pet.CoatTypeId is null ? null : await dbContext.Set<CoatType>().SingleOrDefaultAsync(x => x.Id == pet.Pet.CoatTypeId.Value, cancellationToken);
        var sizeCategory = pet.Pet.SizeCategoryId is null ? null : await dbContext.Set<SizeCategory>().SingleOrDefaultAsync(x => x.Id == pet.Pet.SizeCategoryId.Value, cancellationToken);
        var photos = await dbContext.Set<PetPhoto>()
            .Where(x => x.PetId == petId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new PetPhotoView(x.Id, x.StorageKey, x.FileName, x.ContentType, x.IsPrimary, x.SortOrder, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PetDetailView(
            pet.Pet.Id,
            pet.Pet.ClientId,
            pet.Pet.Name,
            new AnimalTypeView(pet.AnimalType.Id, pet.AnimalType.Code, pet.AnimalType.Name),
            new BreedView(pet.Breed.Id, pet.Breed.AnimalTypeId, pet.Breed.BreedGroupId, pet.Breed.Code, pet.Breed.Name),
            coatType is null ? null : new CoatTypeView(coatType.Id, coatType.AnimalTypeId, coatType.Code, coatType.Name),
            sizeCategory is null ? null : new SizeCategoryView(sizeCategory.Id, sizeCategory.AnimalTypeId, sizeCategory.Code, sizeCategory.Name, sizeCategory.MinWeightKg, sizeCategory.MaxWeightKg),
            pet.Pet.BirthDate,
            pet.Pet.WeightKg,
            pet.Pet.Notes,
            photos,
            [],
            pet.Pet.CreatedAtUtc,
            pet.Pet.UpdatedAtUtc);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record RegisterPetCommand(Guid? ClientId, string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
public sealed record UpdatePetCommand(string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
public sealed record PetCatalogView(IReadOnlyCollection<AnimalTypeView> AnimalTypes, IReadOnlyCollection<BreedGroupView> BreedGroups, IReadOnlyCollection<BreedView> Breeds, IReadOnlyCollection<CoatTypeView> CoatTypes, IReadOnlyCollection<SizeCategoryView> SizeCategories);
public sealed record AnimalTypeView(Guid Id, string Code, string Name);
public sealed record BreedGroupView(Guid Id, Guid AnimalTypeId, string Code, string Name);
public sealed record BreedView(Guid Id, Guid AnimalTypeId, Guid? BreedGroupId, string Code, string Name);
public sealed record CoatTypeView(Guid Id, Guid? AnimalTypeId, string Code, string Name);
public sealed record SizeCategoryView(Guid Id, Guid? AnimalTypeId, string Code, string Name, decimal? MinWeightKg, decimal? MaxWeightKg);
public sealed record PetPhotoView(Guid Id, string StorageKey, string FileName, string ContentType, bool IsPrimary, int SortOrder, DateTime CreatedAtUtc);
public sealed record PetDetailView(Guid Id, Guid? ClientId, string Name, AnimalTypeView AnimalType, BreedView Breed, CoatTypeView? CoatType, SizeCategoryView? SizeCategory, DateOnly? BirthDate, decimal? WeightKg, string? Notes, IReadOnlyCollection<PetPhotoView> Photos, IReadOnlyCollection<PetContactAdminSummary> Contacts, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
