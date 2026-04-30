using ErrorOr;

namespace Tailbook.Modules.Customer.Application.Customer.Queries;

public interface ICustomerQueries
{
    Task<PagedResult<ClientListItemView>> ListClientsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<ClientDetailView?> GetClientDetailAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken);
    Task<ClientDetailView> CreateClientAsync(string displayName, string? notes, CancellationToken cancellationToken);
    Task<ContactPersonView?> AddContactPersonAsync(Guid clientId, string firstName, string? lastName, string? notes, string? trustLevel, CancellationToken cancellationToken);
    Task<ErrorOr<ContactMethodView>> AddContactMethodAsync(Guid contactId, string methodType, string value, string? displayValue, bool isPreferred, string? verificationStatus, string? notes, CancellationToken cancellationToken);
    Task<PetContactLinkView?> LinkContactToPetAsync(Guid petId, Guid contactId, IReadOnlyCollection<string> roleCodes, bool isPrimary, bool canPickUp, bool canPay, bool receivesNotifications, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PetContactLinkView>?> ListPetContactLinksAsync(Guid petId, Guid? actorUserId, CancellationToken cancellationToken);
}
