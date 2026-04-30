using ErrorOr;

namespace Tailbook.Modules.Pets.Application.Pets.Queries;

public interface IPetsQueries
{
    Task<PetCatalogView> GetCatalogAsync(CancellationToken cancellationToken);
    Task<PetDetailView?> GetPetAsync(Guid petId, Guid? actorUserId, bool includeContacts, CancellationToken cancellationToken);
    Task<PagedResult<PetListItemView>> ListPetsAsync(string? search, Guid? clientId, string? animalTypeCode, Guid? breedId, int page, int pageSize, CancellationToken cancellationToken);
    Task<ErrorOr<PetDetailView>> RegisterPetAsync(RegisterPetCommand command, CancellationToken cancellationToken);
    Task<ErrorOr<PetDetailView>> UpdatePetAsync(Guid petId, UpdatePetCommand command, CancellationToken cancellationToken);
}
