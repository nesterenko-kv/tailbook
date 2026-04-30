namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IClientPortalIdentityQueries
{
    Task<LoginResult?> AuthenticateClientAsync(string email, string password, CancellationToken cancellationToken);
}
