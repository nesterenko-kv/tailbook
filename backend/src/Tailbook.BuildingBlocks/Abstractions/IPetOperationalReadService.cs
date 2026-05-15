namespace Tailbook.BuildingBlocks.Abstractions;

public interface IPetOperationalReadService
{
    Task<PetOperationalReadModel?> GetPetOperationalAsync(Guid petId, CancellationToken cancellationToken);
}

public sealed record PetOperationalReadModel(
    Guid Id,
    string Name,
    string AnimalTypeCode,
    string AnimalTypeName,
    string BreedName,
    string? CoatTypeCode,
    string? SizeCategoryCode,
    string? Notes);
