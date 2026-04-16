using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Pets.Domain;

namespace Tailbook.Modules.Pets.Application;

public sealed class PetReferenceServices(AppDbContext dbContext) : IPetReferenceValidationService, IPetReadModelService
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
}
