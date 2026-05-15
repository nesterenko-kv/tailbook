namespace Tailbook.Modules.Pets.Application.Pets.Queries;

public interface IPetsReadService
{
    Task<PetCatalogView> GetCatalogAsync(CancellationToken cancellationToken);
    Task<PetDetailView?> GetPetAsync(Guid petId, Guid? actorUserId, bool includeContacts, CancellationToken cancellationToken);
    Task<PagedResult<PetListItemView>> ListPetsAsync(string? search, Guid? clientId, string? animalTypeCode, Guid? breedId, int page, int pageSize, CancellationToken cancellationToken);
}
