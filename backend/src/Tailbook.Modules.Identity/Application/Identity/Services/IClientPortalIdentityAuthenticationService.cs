namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IClientPortalIdentityAuthenticationService
{
    Task<LoginResult?> AuthenticateClientAsync(string email, string password, CancellationToken cancellationToken);
}
