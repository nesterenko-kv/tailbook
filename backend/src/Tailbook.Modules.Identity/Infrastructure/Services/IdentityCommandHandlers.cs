using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class IdentityCommandHandlers(IdentityUseCases useCases)
    : ICommandHandler<CreateIdentityUserCommand, ErrorOr<UserDetailView>>,
        ICommandHandler<AssignIdentityRolesCommand, ErrorOr<UserDetailView>>
{
    public Task<ErrorOr<UserDetailView>> ExecuteAsync(CreateIdentityUserCommand command, CancellationToken ct = default)
    {
        return useCases.CreateUserAsync(command.Email, command.DisplayName, command.Password, command.RoleCodes, command.AssignedByUserId, ct);
    }

    public Task<ErrorOr<UserDetailView>> ExecuteAsync(AssignIdentityRolesCommand command, CancellationToken ct = default)
    {
        return useCases.AssignRolesAsync(command.UserId, command.RoleCodes, command.AssignedByUserId, ct);
    }
}
