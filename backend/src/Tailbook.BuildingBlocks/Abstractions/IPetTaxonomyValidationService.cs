namespace Tailbook.BuildingBlocks.Abstractions;

public interface IPetTaxonomyValidationService
{
    Task<bool> AnimalTypeExistsAsync(Guid animalTypeId, CancellationToken cancellationToken);
    Task<bool> BreedExistsAsync(Guid breedId, CancellationToken cancellationToken);
    Task<bool> BreedGroupExistsAsync(Guid breedGroupId, CancellationToken cancellationToken);
    Task<bool> CoatTypeExistsAsync(Guid coatTypeId, CancellationToken cancellationToken);
    Task<bool> SizeCategoryExistsAsync(Guid sizeCategoryId, CancellationToken cancellationToken);
}
