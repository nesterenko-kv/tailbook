namespace Tailbook.Modules.Pets.Application.Pets.Queries;

public interface IClientPortalPetsReadService
{
    Task<IReadOnlyCollection<ClientPetSummaryView>> ListMyPetsAsync(Guid clientId, CancellationToken cancellationToken);
    Task<ClientPetDetailView?> GetMyPetAsync(Guid clientId, Guid petId, CancellationToken cancellationToken);
}
