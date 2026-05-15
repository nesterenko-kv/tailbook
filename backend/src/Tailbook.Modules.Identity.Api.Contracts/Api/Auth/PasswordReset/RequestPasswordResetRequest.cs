namespace Tailbook.Modules.Identity.Api.Auth.PasswordReset;

public sealed class RequestPasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}
