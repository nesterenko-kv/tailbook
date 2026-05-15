namespace Tailbook.Modules.Identity.Api.Client.Auth.Login;

public sealed class ClientLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public AuthenticatedUserView User { get; set; } = default!;
}
