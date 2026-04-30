using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IRegisterClientPortalUserHandler
{
    Task<ErrorOr<bool>> ExecuteResultAsync(RegisterClientPortalUserCommand command, CancellationToken cancellationToken);
}
