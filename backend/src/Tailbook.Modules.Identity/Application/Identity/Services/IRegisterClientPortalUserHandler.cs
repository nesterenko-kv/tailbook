using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IRegisterClientPortalUserHandler
{
    Task<ErrorOr<Created>> ExecuteResultAsync(RegisterClientPortalUserInput command, CancellationToken cancellationToken);
}
