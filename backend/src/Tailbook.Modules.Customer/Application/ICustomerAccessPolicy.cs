using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Customer.Application;

public interface ICustomerAccessPolicy
{
    bool CanReadClients(ICurrentUser currentUser);
    bool CanWriteClients(ICurrentUser currentUser);
    bool CanReadContacts(ICurrentUser currentUser);
    bool CanWriteContacts(ICurrentUser currentUser);
}
