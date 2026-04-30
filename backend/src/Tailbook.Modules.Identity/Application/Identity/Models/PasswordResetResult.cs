namespace Tailbook.Modules.Identity.Application.Identity.Models;

public enum PasswordResetResult
{
    Success,
    InvalidToken,
    TokenExpired,
    TokenAlreadyUsed
}
