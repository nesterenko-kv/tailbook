namespace Tailbook.Modules.Identity.Api.Client.Auth.Login;

public sealed class ClientLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
