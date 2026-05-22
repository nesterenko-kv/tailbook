namespace Tailbook.Modules.Identity.Api.Client.Auth.Register;

public sealed class ClientRegisterResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public AuthenticatedUserView User { get; set; } = default!;
}
