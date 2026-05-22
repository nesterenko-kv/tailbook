using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IIdentitySessionService
{
    Task<ErrorOr<LoginResult>> CreateSessionAsync(Guid userId, bool requireClientPortalAccess, CancellationToken cancellationToken);
    Task<ErrorOr<LoginResult>> RefreshSessionAsync(string refreshToken, bool requireClientPortalAccess, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
