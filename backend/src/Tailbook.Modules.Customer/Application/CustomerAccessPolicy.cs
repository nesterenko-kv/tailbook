using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Customer.Application;

public sealed class CustomerAccessPolicy : ICustomerAccessPolicy
{
    private const string ClientsReadPermission = "crm.clients.read";
    private const string ClientsWritePermission = "crm.clients.write";
    private const string ContactsReadPermission = "crm.contacts.read";
    private const string ContactsWritePermission = "crm.contacts.write";

    public bool CanReadClients(ICurrentUser currentUser) => currentUser.HasPermission(ClientsReadPermission);
    public bool CanWriteClients(ICurrentUser currentUser) => currentUser.HasPermission(ClientsWritePermission);
    public bool CanReadContacts(ICurrentUser currentUser) => currentUser.HasPermission(ContactsReadPermission);
    public bool CanWriteContacts(ICurrentUser currentUser) => currentUser.HasPermission(ContactsWritePermission);
}
