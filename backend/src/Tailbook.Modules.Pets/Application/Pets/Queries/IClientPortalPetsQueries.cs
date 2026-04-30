namespace Tailbook.Modules.Pets.Application.Pets.Queries;

public interface IClientPortalPetsQueries
{
    Task<IReadOnlyCollection<ClientPetSummaryView>> ListMyPetsAsync(Guid clientId, CancellationToken cancellationToken);
    Task<ClientPetDetailView?> GetMyPetAsync(Guid clientId, Guid petId, CancellationToken cancellationToken);
}
