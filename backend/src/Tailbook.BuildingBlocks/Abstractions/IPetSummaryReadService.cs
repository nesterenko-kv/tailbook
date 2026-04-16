namespace Tailbook.BuildingBlocks.Abstractions;

public interface IPetSummaryReadService
{
    Task<PetSummaryReadModel?> GetPetSummaryAsync(Guid petId, CancellationToken cancellationToken);
}

public sealed record PetSummaryReadModel(
    Guid Id,
    string Name,
    Guid? ClientId,
    string AnimalTypeCode,
    string AnimalTypeName,
    string BreedName,
    string? CoatTypeCode,
    string? SizeCategoryCode);
