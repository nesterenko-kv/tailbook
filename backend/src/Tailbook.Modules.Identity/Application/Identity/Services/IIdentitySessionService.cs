namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IIdentitySessionService
{
    Task<LoginResult?> RefreshSessionAsync(string refreshToken, bool requireClientPortalAccess, CancellationToken cancellationToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
