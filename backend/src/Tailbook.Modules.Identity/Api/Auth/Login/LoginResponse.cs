using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Auth.Login;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public AuthenticatedUserView User { get; set; } = new(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, [], []);
}
