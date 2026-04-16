using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Pets.Domain;

namespace Tailbook.Modules.Pets.Application;

public sealed class PetReferenceServices(AppDbContext dbContext)
    : IPetReferenceValidationService,
      IPetReadModelService,
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
            .Where(x => x.ClientId == clientId)
            .Join(dbContext.Set<AnimalType>(), x => x.AnimalTypeId, y => y.Id, (x, y) => new { Pet = x, AnimalType = y })
            .Join(dbContext.Set<Breed>(), x => x.Pet.BreedId, y => y.Id, (x, y) => new { x.Pet, x.AnimalType, Breed = y })
            .OrderBy(x => x.Pet.Name)
            .ToListAsync(cancellationToken);

        var coatTypes = await dbContext.Set<CoatType>().ToDictionaryAsync(x => x.Id, cancellationToken);
        var sizeCategories = await dbContext.Set<SizeCategory>().ToDictionaryAsync(x => x.Id, cancellationToken);

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

    public async Task<PetQuoteProfile?> GetPetAsync(Guid petId, CancellationToken cancellationToken)
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

        BreedGroup? breedGroup = null;
        if (pet.Breed.BreedGroupId is not null)
        {
            breedGroup = await dbContext.Set<BreedGroup>().SingleOrDefaultAsync(x => x.Id == pet.Breed.BreedGroupId.Value, cancellationToken);
        }

        CoatType? coatType = null;
        if (pet.Pet.CoatTypeId is not null)
        {
            coatType = await dbContext.Set<CoatType>().SingleOrDefaultAsync(x => x.Id == pet.Pet.CoatTypeId.Value, cancellationToken);
        }

        SizeCategory? sizeCategory = null;
        if (pet.Pet.SizeCategoryId is not null)
        {
            sizeCategory = await dbContext.Set<SizeCategory>().SingleOrDefaultAsync(x => x.Id == pet.Pet.SizeCategoryId.Value, cancellationToken);
        }

        return new PetQuoteProfile(
            pet.Pet.Id,
            pet.Pet.ClientId,
            pet.AnimalType.Id,
            pet.AnimalType.Code,
            pet.AnimalType.Name,
            pet.Breed.Id,
            pet.Breed.Code,
            pet.Breed.Name,
            breedGroup?.Id,
            breedGroup?.Code,
            breedGroup?.Name,
            coatType?.Id,
            coatType?.Code,
            coatType?.Name,
            sizeCategory?.Id,
            sizeCategory?.Code,
            sizeCategory?.Name);
    }

    public async Task<bool> AnimalTypeExistsAsync(Guid animalTypeId, CancellationToken cancellationToken)
        => await dbContext.Set<AnimalType>().AnyAsync(x => x.Id == animalTypeId, cancellationToken);

    public async Task<bool> BreedExistsAsync(Guid breedId, CancellationToken cancellationToken)
        => await dbContext.Set<Breed>().AnyAsync(x => x.Id == breedId, cancellationToken);

    public async Task<bool> BreedGroupExistsAsync(Guid breedGroupId, CancellationToken cancellationToken)
        => await dbContext.Set<BreedGroup>().AnyAsync(x => x.Id == breedGroupId, cancellationToken);

    public async Task<bool> CoatTypeExistsAsync(Guid coatTypeId, CancellationToken cancellationToken)
        => await dbContext.Set<CoatType>().AnyAsync(x => x.Id == coatTypeId, cancellationToken);

    public async Task<bool> SizeCategoryExistsAsync(Guid sizeCategoryId, CancellationToken cancellationToken)
        => await dbContext.Set<SizeCategory>().AnyAsync(x => x.Id == sizeCategoryId, cancellationToken);
}
