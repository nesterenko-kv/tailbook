using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Pets.Infrastructure.Services;

public sealed class PetsUseCases(
    AppDbContext dbContext,
    IClientReferenceValidationService clientReferenceValidationService,
    IPetContactReadModelService petContactReadModelService,
    IAccessAuditService accessAuditService) : IPetsReadService
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

    public async Task<PagedResult<PetListItemView>> ListPetsAsync(
        string? search,
        Guid? clientId,
        string? animalTypeCode,
        Guid? breedId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };
        var normalizedSearch = NormalizeOptional(search);
        var normalizedAnimalTypeCode = NormalizeOptional(animalTypeCode);

        var query = dbContext.Set<Pet>()
            .Join(dbContext.Set<AnimalType>(), pet => pet.AnimalTypeId, animalType => animalType.Id, (pet, animalType) => new { Pet = pet, AnimalType = animalType })
            .Join(dbContext.Set<Breed>(), row => row.Pet.BreedId, breed => breed.Id, (row, breed) => new { row.Pet, row.AnimalType, Breed = breed })
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x => x.Pet.Name.Contains(normalizedSearch));
        }

        if (clientId.HasValue)
        {
            query = query.Where(x => x.Pet.ClientId == clientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedAnimalTypeCode))
        {
            query = query.Where(x => x.AnimalType.Code == normalizedAnimalTypeCode);
        }

        if (breedId.HasValue)
        {
            query = query.Where(x => x.Pet.BreedId == breedId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.Pet.Name)
            .ThenBy(x => x.Pet.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new
            {
                x.Pet.Id,
                x.Pet.ClientId,
                x.Pet.Name,
                AnimalTypeCode = x.AnimalType.Code,
                AnimalTypeName = x.AnimalType.Name,
                BreedName = x.Breed.Name,
                x.Pet.CoatTypeId,
                x.Pet.SizeCategoryId,
                x.Pet.WeightKg,
                x.Pet.CreatedAtUtc,
                x.Pet.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var coatTypeIds = rows.Where(x => x.CoatTypeId.HasValue).Select(x => x.CoatTypeId!.Value).Distinct().ToArray();
        var sizeCategoryIds = rows.Where(x => x.SizeCategoryId.HasValue).Select(x => x.SizeCategoryId!.Value).Distinct().ToArray();
        var coatTypes = await dbContext.Set<CoatType>()
            .Where(x => coatTypeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);
        var sizeCategories = await dbContext.Set<SizeCategory>()
            .Where(x => sizeCategoryIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);

        return new PagedResult<PetListItemView>(
            rows.Select(x => new PetListItemView(
                x.Id,
                x.ClientId,
                x.Name,
                x.AnimalTypeCode,
                x.AnimalTypeName,
                x.BreedName,
                x.CoatTypeId.HasValue ? coatTypes.GetValueOrDefault(x.CoatTypeId.Value) : null,
                x.SizeCategoryId.HasValue ? sizeCategories.GetValueOrDefault(x.SizeCategoryId.Value) : null,
                x.WeightKg,
                x.CreatedAtUtc,
                x.UpdatedAtUtc)).ToArray(),
            safePage,
            safePageSize,
            totalCount);
    }

    public async Task<ErrorOr<PetDetailView>> RegisterPetAsync(RegisterPetCommand command, CancellationToken cancellationToken)
    {
        if (command.ClientId is not null)
        {
            var clientExists = await clientReferenceValidationService.ExistsAsync(command.ClientId.Value, cancellationToken);
            if (!clientExists)
            {
                return Error.NotFound("Pets.ClientNotFound", "Client does not exist.");
            }
        }

        var taxonomy = await ResolvePetTaxonomyAsync(command.AnimalTypeCode, command.BreedId, command.CoatTypeCode, command.SizeCategoryCode, cancellationToken);
        if (taxonomy.IsError)
        {
            return taxonomy.Errors;
        }

        var utcNow = DateTime.UtcNow;
        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            ClientId = command.ClientId,
            Name = command.Name.Trim(),
            AnimalTypeId = taxonomy.Value.AnimalType.Id,
            BreedId = taxonomy.Value.Breed.Id,
            CoatTypeId = taxonomy.Value.CoatType?.Id,
            SizeCategoryId = taxonomy.Value.SizeCategory?.Id,
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

    public async Task<ErrorOr<PetDetailView>> UpdatePetAsync(Guid petId, UpdatePetCommand command, CancellationToken cancellationToken)
    {
        var pet = await dbContext.Set<Pet>().SingleOrDefaultAsync(x => x.Id == petId, cancellationToken);
        if (pet is null)
        {
            return Error.NotFound("Pets.PetNotFound", "Pet does not exist.");
        }

        var taxonomy = await ResolvePetTaxonomyAsync(command.AnimalTypeCode, command.BreedId, command.CoatTypeCode, command.SizeCategoryCode, cancellationToken);
        if (taxonomy.IsError)
        {
            return taxonomy.Errors;
        }

        pet.Name = command.Name.Trim();
        pet.AnimalTypeId = taxonomy.Value.AnimalType.Id;
        pet.BreedId = taxonomy.Value.Breed.Id;
        pet.CoatTypeId = taxonomy.Value.CoatType?.Id;
        pet.SizeCategoryId = taxonomy.Value.SizeCategory?.Id;
        pet.BirthDate = command.BirthDate;
        pet.WeightKg = command.WeightKg;
        pet.Notes = NormalizeOptional(command.Notes);
        pet.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadPetProjectionAsync(pet.Id, cancellationToken))!;
    }

    private async Task<ErrorOr<ResolvedPetTaxonomy>> ResolvePetTaxonomyAsync(
        string animalTypeCode,
        Guid breedId,
        string? coatTypeCode,
        string? sizeCategoryCode,
        CancellationToken cancellationToken)
    {
        var animalType = await dbContext.Set<AnimalType>()
            .SingleOrDefaultAsync(x => x.Code == animalTypeCode, cancellationToken);
        if (animalType is null)
        {
            return Error.Validation("Pets.UnknownAnimalType", $"Unknown animal type '{animalTypeCode}'.");
        }

        var breed = await dbContext.Set<Breed>()
            .SingleOrDefaultAsync(x => x.Id == breedId, cancellationToken);
        if (breed is null)
        {
            return Error.NotFound("Pets.BreedNotFound", "Breed does not exist.");
        }

        if (breed.AnimalTypeId != animalType.Id)
        {
            return Error.Validation("Pets.BreedAnimalTypeMismatch", "Breed must belong to the selected animal type.");
        }

        CoatType? coatType = null;
        if (!string.IsNullOrWhiteSpace(coatTypeCode))
        {
            coatType = await dbContext.Set<CoatType>()
                .SingleOrDefaultAsync(x => x.Code == coatTypeCode, cancellationToken);
            if (coatType is null)
            {
                return Error.Validation("Pets.UnknownCoatType", $"Unknown coat type '{coatTypeCode}'.");
            }

            if (coatType.AnimalTypeId is not null && coatType.AnimalTypeId != animalType.Id)
            {
                return Error.Validation("Pets.CoatTypeAnimalTypeMismatch", "Coat type must belong to the selected animal type when scoped by animal type.");
            }

            var isAllowedForBreed = await dbContext.Set<BreedAllowedCoatType>()
                .AnyAsync(x => x.BreedId == breed.Id && x.CoatTypeId == coatType.Id, cancellationToken);

            if (!isAllowedForBreed)
            {
                return Error.Validation("Pets.CoatTypeNotAllowedForBreed", $"Coat type '{coatType.Name}' is not allowed for breed '{breed.Name}'.");
            }
        }

        SizeCategory? sizeCategory = null;
        if (!string.IsNullOrWhiteSpace(sizeCategoryCode))
        {
            sizeCategory = await dbContext.Set<SizeCategory>()
                .SingleOrDefaultAsync(x => x.Code == sizeCategoryCode, cancellationToken);
            if (sizeCategory is null)
            {
                return Error.Validation("Pets.UnknownSizeCategory", $"Unknown size category '{sizeCategoryCode}'.");
            }

            if (sizeCategory.AnimalTypeId is not null && sizeCategory.AnimalTypeId != animalType.Id)
            {
                return Error.Validation("Pets.SizeCategoryAnimalTypeMismatch", "Size category must belong to the selected animal type when scoped by animal type.");
            }

            var isAllowedForBreed = await dbContext.Set<BreedAllowedSizeCategory>()
                .AnyAsync(x => x.BreedId == breed.Id && x.SizeCategoryId == sizeCategory.Id, cancellationToken);

            if (!isAllowedForBreed)
            {
                return Error.Validation("Pets.SizeCategoryNotAllowedForBreed", $"Size category '{sizeCategory.Name}' is not allowed for breed '{breed.Name}'.");
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
