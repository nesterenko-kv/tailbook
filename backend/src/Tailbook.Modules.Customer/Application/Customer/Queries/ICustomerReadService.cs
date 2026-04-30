namespace Tailbook.Modules.Customer.Application.Customer.Queries;

public interface ICustomerReadService
{
    Task<PagedResult<ClientListItemView>> ListClientsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<ClientDetailView?> GetClientDetailAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PetContactLinkView>?> ListPetContactLinksAsync(Guid petId, Guid? actorUserId, CancellationToken cancellationToken);
}
