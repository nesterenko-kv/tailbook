using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Pets.Infrastructure.Services;

public sealed class ClientPortalPetsQueries(AppDbContext dbContext)
{
    public async Task<IReadOnlyCollection<ClientPetSummaryView>> ListMyPetsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var pets = await dbContext.Set<Pet>()
            .Where(x => x.ClientId == clientId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (pets.Count == 0)
        {
            return [];
        }

        var animalTypes = await dbContext.Set<AnimalType>().ToDictionaryAsync(x => x.Id, cancellationToken);
        var breeds = await dbContext.Set<Breed>().ToDictionaryAsync(x => x.Id, cancellationToken);
        var coatTypes = await dbContext.Set<CoatType>().ToDictionaryAsync(x => x.Id, cancellationToken);
        var sizeCategories = await dbContext.Set<SizeCategory>().ToDictionaryAsync(x => x.Id, cancellationToken);
        var petIds = pets.Select(x => x.Id).ToArray();
        var photoMap = await dbContext.Set<PetPhoto>()
            .Where(x => petIds.Contains(x.PetId))
            .GroupBy(x => x.PetId)
            .Select(x => new { PetId = x.Key, Primary = x.OrderByDescending(y => y.IsPrimary).ThenBy(y => y.SortOrder).Select(y => y.FileName).FirstOrDefault() })
            .ToDictionaryAsync(x => x.PetId, x => x.Primary, cancellationToken);

        return pets.Select(pet => new ClientPetSummaryView(
            pet.Id,
            pet.Name,
            animalTypes[pet.AnimalTypeId].Code,
            animalTypes[pet.AnimalTypeId].Name,
            breeds[pet.BreedId].Name,
            pet.CoatTypeId is null ? null : coatTypes[pet.CoatTypeId.Value].Code,
            pet.SizeCategoryId is null ? null : sizeCategories[pet.SizeCategoryId.Value].Code,
            pet.Notes,
            photoMap.GetValueOrDefault(pet.Id))).ToArray();
    }

    public async Task<ClientPetDetailView?> GetMyPetAsync(Guid clientId, Guid petId, CancellationToken cancellationToken)
    {
        var pet = await dbContext.Set<Pet>()
            .SingleOrDefaultAsync(x => x.Id == petId && x.ClientId == clientId, cancellationToken);

        if (pet is null)
        {
            return null;
        }

        var animalType = await dbContext.Set<AnimalType>().SingleAsync(x => x.Id == pet.AnimalTypeId, cancellationToken);
        var breed = await dbContext.Set<Breed>().SingleAsync(x => x.Id == pet.BreedId, cancellationToken);
        var coatType = pet.CoatTypeId is null ? null : await dbContext.Set<CoatType>().SingleOrDefaultAsync(x => x.Id == pet.CoatTypeId.Value, cancellationToken);
        var sizeCategory = pet.SizeCategoryId is null ? null : await dbContext.Set<SizeCategory>().SingleOrDefaultAsync(x => x.Id == pet.SizeCategoryId.Value, cancellationToken);
        var photos = await dbContext.Set<PetPhoto>()
            .Where(x => x.PetId == pet.Id)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .Select(x => new ClientPetPhotoView(x.Id, x.FileName, x.ContentType, x.IsPrimary, x.SortOrder))
            .ToListAsync(cancellationToken);

        return new ClientPetDetailView(
            pet.Id,
            pet.Name,
            animalType.Code,
            animalType.Name,
            breed.Name,
            coatType?.Code,
            sizeCategory?.Code,
            pet.BirthDate,
            pet.WeightKg,
            pet.Notes,
            photos);
    }
}
