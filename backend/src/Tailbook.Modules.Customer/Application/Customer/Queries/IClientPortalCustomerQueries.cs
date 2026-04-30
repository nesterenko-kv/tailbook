using ErrorOr;

namespace Tailbook.Modules.Customer.Application.Customer.Queries;

public interface IClientPortalCustomerQueries
{
    Task<ClientContactPreferencesView?> GetContactPreferencesAsync(Guid contactPersonId, CancellationToken cancellationToken);
    Task<ErrorOr<ClientContactPreferencesView>> UpdateContactPreferencesAsync(Guid contactPersonId, UpdateClientContactPreferencesCommand command, CancellationToken cancellationToken);
}
