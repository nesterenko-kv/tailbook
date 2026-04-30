namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IPasswordResetService
{
    Task RequestResetAsync(string email, CancellationToken cancellationToken);
    Task<PasswordResetResult> ResetPasswordAsync(string rawToken, string newPassword, CancellationToken cancellationToken);
}
