namespace Tailbook.Modules.Identity.Api.Auth.Refresh;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
