using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IAuthenticateUserService
{
    Task<ErrorOr<AuthenticationResult>> AuthenticateAsync(
        string email,
        string password,
        bool requireClientPortalAccess,
        bool enforceMfa,
        string? requestIpAddress,
        string? userAgent,
        string? deviceTrustToken,
        CancellationToken cancellationToken);
}
