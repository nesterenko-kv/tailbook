namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IAuthenticateUserService
{
    Task<LoginResult?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
}
