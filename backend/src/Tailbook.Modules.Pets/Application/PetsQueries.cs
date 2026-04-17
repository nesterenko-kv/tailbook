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
        var animalTypes = await dbContext.Set<AnimalType>()
            .OrderBy(x => x.Name)
            .Select(x => new AnimalTypeView(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        var breedGroups = await dbContext.Set<BreedGroup>()
            .OrderBy(x => x.Name)
            .Select(x => new BreedGroupView(x.Id, x.AnimalTypeId, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        var breedAllowedCoatTypes = await dbContext.Set<BreedAllowedCoatType>()
            .ToListAsync(cancellationToken);
        var allowedCoatTypesByBreedId = breedAllowedCoatTypes
            .GroupBy(x => x.BreedId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyCollection<Guid>)x.Select(y => y.CoatTypeId).OrderBy(y => y).ToArray());

        var breedAllowedSizeCategories = await dbContext.Set<BreedAllowedSizeCategory>()
            .ToListAsync(cancellationToken);
        var allowedSizeCategoriesByBreedId = breedAllowedSizeCategories
            .GroupBy(x => x.BreedId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyCollection<Guid>)x.Select(y => y.SizeCategoryId).OrderBy(y => y).ToArray());

        var breeds = (await dbContext.Set<Breed>()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.AnimalTypeId, x.BreedGroupId, x.Code, x.Name })
                .ToListAsync(cancellationToken))
            .Select(x => new BreedView(
                x.Id,
                x.AnimalTypeId,
                x.BreedGroupId,
                x.Code,
                x.Name,
                allowedCoatTypesByBreedId.TryGetValue(x.Id, out var allowedCoatTypeIds) ? allowedCoatTypeIds : Array.Empty<Guid>(),
                allowedSizeCategoriesByBreedId.TryGetValue(x.Id, out var allowedSizeCategoryIds) ? allowedSizeCategoryIds : Array.Empty<Guid>()))
            .ToList();

        var coatTypes = await dbContext.Set<CoatType>()
            .OrderBy(x => x.Name)
            .Select(x => new CoatTypeView(x.Id, x.AnimalTypeId, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        var supportedSizeCategoryIds = allowedSizeCategoriesByBreedId.Values
            .SelectMany(x => x)
            .Distinct()
            .ToArray();

        var sizeCategories = await dbContext.Set<SizeCategory>()
            .Where(x => supportedSizeCategoryIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new SizeCategoryView(x.Id, x.AnimalTypeId, x.Code, x.Name, x.MinWeightKg, x.MaxWeightKg))
            .ToListAsync(cancellationToken);

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

        var taxonomy = await ResolvePetTaxonomyAsync(command.AnimalTypeCode, command.BreedId, command.CoatTypeCode, command.SizeCategoryCode, cancellationToken);
        var utcNow = DateTime.UtcNow;
        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            ClientId = command.ClientId,
            Name = command.Name.Trim(),
            AnimalTypeId = taxonomy.AnimalType.Id,
            BreedId = taxonomy.Breed.Id,
            CoatTypeId = taxonomy.CoatType?.Id,
            SizeCategoryId = taxonomy.SizeCategory?.Id,
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

        var taxonomy = await ResolvePetTaxonomyAsync(command.AnimalTypeCode, command.BreedId, command.CoatTypeCode, command.SizeCategoryCode, cancellationToken);

        pet.Name = command.Name.Trim();
        pet.AnimalTypeId = taxonomy.AnimalType.Id;
        pet.BreedId = taxonomy.Breed.Id;
        pet.CoatTypeId = taxonomy.CoatType?.Id;
        pet.SizeCategoryId = taxonomy.SizeCategory?.Id;
        pet.BirthDate = command.BirthDate;
        pet.WeightKg = command.WeightKg;
        pet.Notes = NormalizeOptional(command.Notes);
        pet.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadPetProjectionAsync(pet.Id, cancellationToken);
    }

    private async Task<ResolvedPetTaxonomy> ResolvePetTaxonomyAsync(
        string animalTypeCode,
        Guid breedId,
        string? coatTypeCode,
        string? sizeCategoryCode,
        CancellationToken cancellationToken)
    {
        var animalType = await dbContext.Set<AnimalType>()
            .SingleOrDefaultAsync(x => x.Code == animalTypeCode, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown animal type '{animalTypeCode}'.");

        var breed = await dbContext.Set<Breed>()
            .SingleOrDefaultAsync(x => x.Id == breedId, cancellationToken)
            ?? throw new InvalidOperationException("Breed does not exist.");

        if (breed.AnimalTypeId != animalType.Id)
        {
            throw new InvalidOperationException("Breed must belong to the selected animal type.");
        }

        CoatType? coatType = null;
        if (!string.IsNullOrWhiteSpace(coatTypeCode))
        {
            coatType = await dbContext.Set<CoatType>()
                .SingleOrDefaultAsync(x => x.Code == coatTypeCode, cancellationToken)
                ?? throw new InvalidOperationException($"Unknown coat type '{coatTypeCode}'.");

            if (coatType.AnimalTypeId is not null && coatType.AnimalTypeId != animalType.Id)
            {
                throw new InvalidOperationException("Coat type must belong to the selected animal type when scoped by animal type.");
            }

            var isAllowedForBreed = await dbContext.Set<BreedAllowedCoatType>()
                .AnyAsync(x => x.BreedId == breed.Id && x.CoatTypeId == coatType.Id, cancellationToken);

            if (!isAllowedForBreed)
            {
                throw new InvalidOperationException($"Coat type '{coatType.Name}' is not allowed for breed '{breed.Name}'.");
            }
        }

        SizeCategory? sizeCategory = null;
        if (!string.IsNullOrWhiteSpace(sizeCategoryCode))
        {
            sizeCategory = await dbContext.Set<SizeCategory>()
                .SingleOrDefaultAsync(x => x.Code == sizeCategoryCode, cancellationToken)
                ?? throw new InvalidOperationException($"Unknown size category '{sizeCategoryCode}'.");

            if (sizeCategory.AnimalTypeId is not null && sizeCategory.AnimalTypeId != animalType.Id)
            {
                throw new InvalidOperationException("Size category must belong to the selected animal type when scoped by animal type.");
            }

            var isAllowedForBreed = await dbContext.Set<BreedAllowedSizeCategory>()
                .AnyAsync(x => x.BreedId == breed.Id && x.SizeCategoryId == sizeCategory.Id, cancellationToken);

            if (!isAllowedForBreed)
            {
                throw new InvalidOperationException($"Size category '{sizeCategory.Name}' is not allowed for breed '{breed.Name}'.");
            }
        }

        return new ResolvedPetTaxonomy(animalType, breed, coatType, sizeCategory);
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

        var coatType = pet.Pet.CoatTypeId is null
            ? null
            : await dbContext.Set<CoatType>().SingleOrDefaultAsync(x => x.Id == pet.Pet.CoatTypeId.Value, cancellationToken);
        var sizeCategory = pet.Pet.SizeCategoryId is null
            ? null
            : await dbContext.Set<SizeCategory>().SingleOrDefaultAsync(x => x.Id == pet.Pet.SizeCategoryId.Value, cancellationToken);
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
            new BreedView(pet.Breed.Id, pet.Breed.AnimalTypeId, pet.Breed.BreedGroupId, pet.Breed.Code, pet.Breed.Name, Array.Empty<Guid>(), Array.Empty<Guid>()),
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

    private sealed record ResolvedPetTaxonomy(AnimalType AnimalType, Breed Breed, CoatType? CoatType, SizeCategory? SizeCategory);
}

public sealed record RegisterPetCommand(Guid? ClientId, string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
public sealed record UpdatePetCommand(string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
public sealed record PetCatalogView(IReadOnlyCollection<AnimalTypeView> AnimalTypes, IReadOnlyCollection<BreedGroupView> BreedGroups, IReadOnlyCollection<BreedView> Breeds, IReadOnlyCollection<CoatTypeView> CoatTypes, IReadOnlyCollection<SizeCategoryView> SizeCategories);
public sealed record AnimalTypeView(Guid Id, string Code, string Name);
public sealed record BreedGroupView(Guid Id, Guid AnimalTypeId, string Code, string Name);
public sealed record BreedView(Guid Id, Guid AnimalTypeId, Guid? BreedGroupId, string Code, string Name, IReadOnlyCollection<Guid> AllowedCoatTypeIds, IReadOnlyCollection<Guid> AllowedSizeCategoryIds);
public sealed record CoatTypeView(Guid Id, Guid? AnimalTypeId, string Code, string Name);
public sealed record SizeCategoryView(Guid Id, Guid? AnimalTypeId, string Code, string Name, decimal? MinWeightKg, decimal? MaxWeightKg);
public sealed record PetPhotoView(Guid Id, string StorageKey, string FileName, string ContentType, bool IsPrimary, int SortOrder, DateTime CreatedAtUtc);
public sealed record PetDetailView(Guid Id, Guid? ClientId, string Name, AnimalTypeView AnimalType, BreedView Breed, CoatTypeView? CoatType, SizeCategoryView? SizeCategory, DateOnly? BirthDate, decimal? WeightKg, string? Notes, IReadOnlyCollection<PetPhotoView> Photos, IReadOnlyCollection<PetContactAdminSummary> Contacts, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
