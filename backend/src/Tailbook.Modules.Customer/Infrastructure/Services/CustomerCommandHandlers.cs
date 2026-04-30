using ErrorOr;
using FastEndpoints;
using Tailbook.Modules.Customer.Application.Customer.Commands;

namespace Tailbook.Modules.Customer.Infrastructure.Services;

public sealed class CustomerCommandHandlers(
    CustomerUseCases customerUseCases,
    ClientPortalCustomerUseCases clientPortalCustomerUseCases)
    : ICommandHandler<CreateCustomerClientCommand, ClientDetailView>,
      ICommandHandler<AddCustomerContactPersonCommand, ContactPersonView?>,
      ICommandHandler<AddCustomerContactMethodCommand, ErrorOr<ContactMethodView>>,
      ICommandHandler<LinkCustomerContactToPetCommand, PetContactLinkView?>,
      ICommandHandler<UpdateClientContactPreferencesUseCaseCommand, ErrorOr<ClientContactPreferencesView>>
{
    public Task<ClientDetailView> ExecuteAsync(CreateCustomerClientCommand command, CancellationToken cancellationToken)
        => customerUseCases.CreateClientAsync(command.DisplayName, command.Notes, cancellationToken);

    public Task<ContactPersonView?> ExecuteAsync(AddCustomerContactPersonCommand command, CancellationToken cancellationToken)
        => customerUseCases.AddContactPersonAsync(command.ClientId, command.FirstName, command.LastName, command.Notes, command.TrustLevel, cancellationToken);

    public Task<ErrorOr<ContactMethodView>> ExecuteAsync(AddCustomerContactMethodCommand command, CancellationToken cancellationToken)
        => customerUseCases.AddContactMethodAsync(command.ContactId, command.MethodType, command.Value, command.DisplayValue, command.IsPreferred, command.VerificationStatus, command.Notes, cancellationToken);

    public Task<PetContactLinkView?> ExecuteAsync(LinkCustomerContactToPetCommand command, CancellationToken cancellationToken)
        => customerUseCases.LinkContactToPetAsync(command.PetId, command.ContactId, command.RoleCodes, command.IsPrimary, command.CanPickUp, command.CanPay, command.ReceivesNotifications, cancellationToken);

    public Task<ErrorOr<ClientContactPreferencesView>> ExecuteAsync(UpdateClientContactPreferencesUseCaseCommand command, CancellationToken cancellationToken)
        => clientPortalCustomerUseCases.UpdateContactPreferencesAsync(command.ContactPersonId, command.Preferences, cancellationToken);
}
