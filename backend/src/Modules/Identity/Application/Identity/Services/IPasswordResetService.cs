using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IPasswordResetService
{
    Task RequestResetAsync(string email, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> ResetPasswordAsync(string rawToken, string newPassword, CancellationToken cancellationToken);
}
