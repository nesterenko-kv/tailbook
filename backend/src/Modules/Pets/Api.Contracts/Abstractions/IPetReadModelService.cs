namespace Tailbook.Modules.Pets.Api.Contracts.Abstractions;

public interface IPetReadModelService
{
    Task<IReadOnlyCollection<PetAdminSummary>> GetPetsByClientAsync(Guid clientId, CancellationToken cancellationToken);
}

public sealed record PetAdminSummary(
    Guid Id,
    string Name,
    string AnimalTypeCode,
    string AnimalTypeName,
    string BreedName,
    string? CoatTypeCode,
    string? SizeCategoryCode
);
