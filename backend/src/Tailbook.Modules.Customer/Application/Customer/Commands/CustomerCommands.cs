using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Customer.Application.Customer.Commands;

public sealed record CreateCustomerClientCommand(string DisplayName, string? Notes) : ICommand<ClientDetailView>;

public sealed record AddCustomerContactPersonCommand(
    Guid ClientId,
    string FirstName,
    string? LastName,
    string? Notes,
    string? TrustLevel) : ICommand<ContactPersonView?>;

public sealed record AddCustomerContactMethodCommand(
    Guid ContactId,
    string MethodType,
    string Value,
    string? DisplayValue,
    bool IsPreferred,
    string? VerificationStatus,
    string? Notes) : ICommand<ErrorOr<ContactMethodView>>;

public sealed record LinkCustomerContactToPetCommand(
    Guid PetId,
    Guid ContactId,
    IReadOnlyCollection<string> RoleCodes,
    bool IsPrimary,
    bool CanPickUp,
    bool CanPay,
    bool ReceivesNotifications) : ICommand<PetContactLinkView?>;

public sealed record UpdateClientContactPreferencesUseCaseCommand(
    Guid ContactPersonId,
    UpdateClientContactPreferencesCommand Preferences) : ICommand<ErrorOr<ClientContactPreferencesView>>;
