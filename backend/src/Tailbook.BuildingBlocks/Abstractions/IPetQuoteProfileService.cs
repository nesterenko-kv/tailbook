namespace Tailbook.BuildingBlocks.Abstractions;

public interface IPetQuoteProfileService
{
    Task<PetQuoteProfile?> GetPetAsync(Guid petId, CancellationToken cancellationToken);
}

public sealed record PetQuoteProfile(
    Guid Id,
    Guid? ClientId,
    Guid AnimalTypeId,
    string AnimalTypeCode,
    string AnimalTypeName,
    Guid BreedId,
    string BreedCode,
    string BreedName,
    Guid? BreedGroupId,
    string? BreedGroupCode,
    string? BreedGroupName,
    Guid? CoatTypeId,
    string? CoatTypeCode,
    string? CoatTypeName,
    Guid? SizeCategoryId,
    string? SizeCategoryCode,
    string? SizeCategoryName);
