namespace Tailbook.Modules.Identity.Api.IssueDevelopmentToken;

public sealed class IssueDevelopmentTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
}
