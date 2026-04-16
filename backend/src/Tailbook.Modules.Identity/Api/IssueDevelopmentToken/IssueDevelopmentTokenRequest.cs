namespace Tailbook.Modules.Identity.Api.IssueDevelopmentToken;

public sealed class IssueDevelopmentTokenRequest
{
    public string SubjectId { get; init; } = "dev-admin";
    public string Email { get; init; } = "admin@tailbook.local";
    public string[] Roles { get; init; } = ["Admin"];
}
