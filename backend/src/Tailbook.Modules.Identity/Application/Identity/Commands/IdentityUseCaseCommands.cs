using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Identity.Application.Identity.Commands;

public sealed record CreateIdentityUserCommand(
    string Email,
    string DisplayName,
    string Password,
    IReadOnlyCollection<string> RoleCodes,
    Guid? AssignedByUserId) : ICommand<ErrorOr<UserDetailView>>;

public sealed record AssignIdentityRolesCommand(
    Guid UserId,
    IReadOnlyCollection<string> RoleCodes,
    Guid? AssignedByUserId) : ICommand<ErrorOr<UserDetailView>>;
