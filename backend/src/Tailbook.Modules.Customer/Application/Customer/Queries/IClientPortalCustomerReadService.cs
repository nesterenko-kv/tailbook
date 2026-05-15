namespace Tailbook.Modules.Customer.Application.Customer.Queries;

public interface IClientPortalCustomerReadService
{
    Task<ClientContactPreferencesView?> GetContactPreferencesAsync(Guid contactPersonId, CancellationToken cancellationToken);
}
