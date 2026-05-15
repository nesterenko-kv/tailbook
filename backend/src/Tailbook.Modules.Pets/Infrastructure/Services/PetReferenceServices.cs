using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Search;

namespace Tailbook.Modules.Pets.Infrastructure.Services;

public sealed class PetReferenceServices(AppDbContext dbContext, IDistributedCache cache)
    : IPetReferenceValidationService,
      IPetReadModelService,
      IPetSummaryReadService,
      IPetOperationalReadService,
      IPetQuoteProfileService,
      IPetTaxonomyValidationService
{
    public async Task<bool> ExistsAsync(Guid petId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Pet>().AnyAsync(x => x.Id == petId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PetAdminSummary>> GetPetsByClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<Pet>()
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .Join(dbContext.Set<AnimalType>().AsNoTracking(), x => x.AnimalTypeId, y => y.Id, (x, y) => new { Pet = x, AnimalType = y })
            .Join(dbContext.Set<Breed>().AsNoTracking(), x => x.Pet.BreedId, y => y.Id, (x, y) => new { x.Pet, x.AnimalType, Breed = y })
            .OrderBy(x => x.Pet.Name)
            .ToListAsync(cancellationToken);

        var coatTypes = await dbContext.Set<CoatType>().AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var sizeCategories = await dbContext.Set<SizeCategory>().AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);

        return items.Select(x => new PetAdminSummary(
                x.Pet.Id,
                x.Pet.Name,
                x.AnimalType.Code,
                x.AnimalType.Name,
                x.Breed.Name,
                x.Pet.CoatTypeId is not null && coatTypes.TryGetValue(x.Pet.CoatTypeId.Value, out var coatType) ? coatType.Code : null,
                x.Pet.SizeCategoryId is not null && sizeCategories.TryGetValue(x.Pet.SizeCategoryId.Value, out var sizeCategory) ? sizeCategory.Code : null))
            .ToArray();
    }

    public async Task<PetSummaryReadModel?> GetPetSummaryAsync(Guid petId, CancellationToken cancellationToken)
    {
        return await QueryPetSummaries()
            .SingleOrDefaultAsync(x => x.Id == petId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PetSummaryReadModel>> ListPetSummariesByClientAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await QueryPetSummaries()
            .Where(x => x.ClientId == clientId)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> SearchPetIdsAsync(string? search, int maxResults, CancellationToken cancellationToken)
    {
        var terms = SearchText.Terms(search);
        if (terms.Length == 0)
        {
            return [];
        }

        var safeMaxResults = maxResults switch
        {
            <= 0 => 100,
            > 1000 => 1000,
            _ => maxResults
        };

        var query =
            from pet in dbContext.Set<Pet>().AsNoTracking()
            join animalType in dbContext.Set<AnimalType>().AsNoTracking()
                on pet.AnimalTypeId equals animalType.Id
            join breed in dbContext.Set<Breed>().AsNoTracking()
                on pet.BreedId equals breed.Id
            select new { Pet = pet, AnimalType = animalType, Breed = breed };

        foreach (var term in terms)
        {
            if (dbContext.Database.IsNpgsql())
            {
                var pattern = SearchText.LikePattern(term);
                query = query.Where(x =>
                    EF.Functions.ILike(x.Pet.Name, pattern, @"\") ||
                    (x.Pet.Notes != null && EF.Functions.ILike(x.Pet.Notes, pattern, @"\")) ||
                    EF.Functions.ILike(x.AnimalType.Code, pattern, @"\") ||
                    EF.Functions.ILike(x.AnimalType.Name, pattern, @"\") ||
                    EF.Functions.ILike(x.Breed.Code, pattern, @"\") ||
                    EF.Functions.ILike(x.Breed.Name, pattern, @"\"));
            }
            else
            {
                query = query.Where(x =>
                    x.Pet.Name.ToLower().Contains(term) ||
                    (x.Pet.Notes != null && x.Pet.Notes.ToLower().Contains(term)) ||
                    x.AnimalType.Code.ToLower().Contains(term) ||
                    x.AnimalType.Name.ToLower().Contains(term) ||
                    x.Breed.Code.ToLower().Contains(term) ||
                    x.Breed.Name.ToLower().Contains(term));
            }
        }

        return await query
            .OrderBy(x => x.Pet.Name)
            .Select(x => x.Pet.Id)
            .Take(safeMaxResults)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PetOperationalReadModel?> GetPetOperationalAsync(Guid petId, CancellationToken cancellationToken)
    {
        var summary = await GetPetSummaryAsync(petId, cancellationToken);
        if (summary is null)
        {
            return null;
        }

        var notes = await dbContext.Set<Pet>()
            .AsNoTracking()
            .Where(x => x.Id == petId)
            .Select(x => x.Notes)
            .SingleAsync(cancellationToken);

        return new PetOperationalReadModel(
            summary.Id,
            summary.Name,
            summary.AnimalTypeCode,
            summary.AnimalTypeName,
            summary.BreedName,
            summary.CoatTypeCode,
            summary.SizeCategoryCode,
            notes);
    }

    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions PetProfileCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public async Task<PetQuoteProfile?> GetPetAsync(Guid petId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.PetProfile(petId);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<PetQuoteProfile>(cached, CacheJsonOptions);
        }

        var pet = await dbContext.Set<Pet>()
            .AsNoTracking()
            .Where(x => x.Id == petId)
            .Join(dbContext.Set<AnimalType>().AsNoTracking(), x => x.AnimalTypeId, y => y.Id, (x, y) => new { Pet = x, AnimalType = y })
            .Join(dbContext.Set<Breed>().AsNoTracking(), x => x.Pet.BreedId, y => y.Id, (x, y) => new { x.Pet, x.AnimalType, Breed = y })
            .SingleOrDefaultAsync(cancellationToken);

        if (pet is null)
        {
            return null;
        }

        var taxonomyResult = await ResolveQuoteTaxonomyAsync(
            pet.AnimalType.Id,
            pet.Breed.Id,
            pet.Pet.CoatTypeId,
            pet.Pet.SizeCategoryId,
            cancellationToken);
        if (taxonomyResult.IsError)
        {
            return null;
        }

        var taxonomy = taxonomyResult.Value;

        var profile = new PetQuoteProfile(
            pet.Pet.Id,
            pet.Pet.ClientId,
            taxonomy.AnimalType.Id,
            taxonomy.AnimalType.Code,
            taxonomy.AnimalType.Name,
            taxonomy.Breed.Id,
            taxonomy.Breed.Code,
            taxonomy.Breed.Name,
            taxonomy.BreedGroup?.Id,
            taxonomy.BreedGroup?.Code,
            taxonomy.BreedGroup?.Name,
            taxonomy.CoatType?.Id,
            taxonomy.CoatType?.Code,
            taxonomy.CoatType?.Name,
            taxonomy.SizeCategory?.Id,
            taxonomy.SizeCategory?.Code,
            taxonomy.SizeCategory?.Name);

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(profile, CacheJsonOptions), PetProfileCacheOptions, cancellationToken);

        return profile;
    }

    public async Task<ErrorOr<PetQuoteProfile>> CreateAdHocAsync(PetQuoteProfileInput input, CancellationToken cancellationToken)
    {
        var taxonomyResult = await ResolveQuoteTaxonomyAsync(
            input.AnimalTypeId,
            input.BreedId,
            input.CoatTypeId,
            input.SizeCategoryId,
            cancellationToken);

        if (taxonomyResult.IsError)
        {
            return taxonomyResult.Errors;
        }

        var taxonomy = taxonomyResult.Value;

        return new PetQuoteProfile(
            Guid.Empty,
            null,
            taxonomy.AnimalType.Id,
            taxonomy.AnimalType.Code,
            taxonomy.AnimalType.Name,
            taxonomy.Breed.Id,
            taxonomy.Breed.Code,
            taxonomy.Breed.Name,
            taxonomy.BreedGroup?.Id,
            taxonomy.BreedGroup?.Code,
            taxonomy.BreedGroup?.Name,
            taxonomy.CoatType?.Id,
            taxonomy.CoatType?.Code,
            taxonomy.CoatType?.Name,
            taxonomy.SizeCategory?.Id,
            taxonomy.SizeCategory?.Code,
            taxonomy.SizeCategory?.Name);
    }

    private async Task<ErrorOr<ResolvedQuoteTaxonomy>> ResolveQuoteTaxonomyAsync(
        Guid animalTypeId,
        Guid breedId,
        Guid? coatTypeId,
        Guid? sizeCategoryId,
        CancellationToken cancellationToken)
    {
        var animalType = await dbContext.Set<AnimalType>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == animalTypeId, cancellationToken);
        if (animalType is null)
        {
            return Error.NotFound("Pets.AnimalTypeNotFound", "Animal type does not exist.");
        }

        var breed = await dbContext.Set<Breed>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == breedId, cancellationToken);
        if (breed is null)
        {
            return Error.NotFound("Pets.BreedNotFound", "Breed does not exist.");
        }

        if (breed.AnimalTypeId != animalType.Id)
        {
            return Error.Validation("Pets.BreedAnimalTypeMismatch", "Breed must belong to the selected animal type.");
        }

        BreedGroup? breedGroup = null;
        if (breed.BreedGroupId is not null)
        {
            breedGroup = await dbContext.Set<BreedGroup>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == breed.BreedGroupId.Value, cancellationToken);
        }

        CoatType? coatType = null;
        if (coatTypeId is not null)
        {
            coatType = await dbContext.Set<CoatType>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == coatTypeId.Value, cancellationToken);
            if (coatType is null)
            {
                return Error.NotFound("Pets.CoatTypeNotFound", "Coat type does not exist.");
            }

            if (coatType.AnimalTypeId is not null && coatType.AnimalTypeId != animalType.Id)
            {
                return Error.Validation("Pets.CoatTypeAnimalTypeMismatch", "Coat type must belong to the selected animal type when scoped by animal type.");
            }

            var isAllowedForBreed = await dbContext.Set<BreedAllowedCoatType>()
                .AnyAsync(x => x.BreedId == breed.Id && x.CoatTypeId == coatType.Id, cancellationToken);

            if (!isAllowedForBreed)
            {
                return Error.Validation("Pets.CoatTypeBreedMismatch", $"Coat type '{coatType.Name}' is not allowed for breed '{breed.Name}'.");
            }
        }

        SizeCategory? sizeCategory = null;
        if (sizeCategoryId is not null)
        {
            sizeCategory = await dbContext.Set<SizeCategory>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == sizeCategoryId.Value, cancellationToken);
            if (sizeCategory is null)
            {
                return Error.NotFound("Pets.SizeCategoryNotFound", "Size category does not exist.");
            }

            if (sizeCategory.AnimalTypeId is not null && sizeCategory.AnimalTypeId != animalType.Id)
            {
                return Error.Validation("Pets.SizeCategoryAnimalTypeMismatch", "Size category must belong to the selected animal type when scoped by animal type.");
            }

            var isAllowedForBreed = await dbContext.Set<BreedAllowedSizeCategory>()
                .AnyAsync(x => x.BreedId == breed.Id && x.SizeCategoryId == sizeCategory.Id, cancellationToken);

            if (!isAllowedForBreed)
            {
                return Error.Validation("Pets.SizeCategoryBreedMismatch", $"Size category '{sizeCategory.Name}' is not allowed for breed '{breed.Name}'.");
            }
        }

        return new ResolvedQuoteTaxonomy(animalType, breed, breedGroup, coatType, sizeCategory);
    }

    public async Task<bool> AnimalTypeExistsAsync(Guid animalTypeId, CancellationToken cancellationToken)
        => await dbContext.Set<AnimalType>().AsNoTracking().AnyAsync(x => x.Id == animalTypeId, cancellationToken);

    public async Task<bool> BreedExistsAsync(Guid breedId, CancellationToken cancellationToken)
        => await dbContext.Set<Breed>().AsNoTracking().AnyAsync(x => x.Id == breedId, cancellationToken);

    public async Task<bool> BreedGroupExistsAsync(Guid breedGroupId, CancellationToken cancellationToken)
        => await dbContext.Set<BreedGroup>().AsNoTracking().AnyAsync(x => x.Id == breedGroupId, cancellationToken);

    public async Task<bool> CoatTypeExistsAsync(Guid coatTypeId, CancellationToken cancellationToken)
        => await dbContext.Set<CoatType>().AsNoTracking().AnyAsync(x => x.Id == coatTypeId, cancellationToken);

    public async Task<bool> SizeCategoryExistsAsync(Guid sizeCategoryId, CancellationToken cancellationToken)
        => await dbContext.Set<SizeCategory>().AsNoTracking().AnyAsync(x => x.Id == sizeCategoryId, cancellationToken);

    private IQueryable<PetSummaryReadModel> QueryPetSummaries()
    {
        return
            from pet in dbContext.Set<Pet>().AsNoTracking()
            join animalType in dbContext.Set<AnimalType>().AsNoTracking()
                on pet.AnimalTypeId equals animalType.Id
            join breed in dbContext.Set<Breed>().AsNoTracking()
                on pet.BreedId equals breed.Id
            join coatType in dbContext.Set<CoatType>().AsNoTracking()
                on pet.CoatTypeId equals coatType.Id into coatTypes
            from coatType in coatTypes.DefaultIfEmpty()
            join sizeCategory in dbContext.Set<SizeCategory>().AsNoTracking()
                on pet.SizeCategoryId equals sizeCategory.Id into sizeCategories
            from sizeCategory in sizeCategories.DefaultIfEmpty()
            select new PetSummaryReadModel(
                pet.Id,
                pet.Name,
                pet.ClientId,
                animalType.Code,
                animalType.Name,
                breed.Name,
                coatType == null ? null : coatType.Code,
                sizeCategory == null ? null : sizeCategory.Code);
    }

    private sealed record ResolvedPetTaxonomy(AnimalType AnimalType, Breed Breed, CoatType? CoatType, SizeCategory? SizeCategory);
    private sealed record ResolvedQuoteTaxonomy(AnimalType AnimalType, Breed Breed, BreedGroup? BreedGroup, CoatType? CoatType, SizeCategory? SizeCategory);
}
