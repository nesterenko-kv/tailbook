
namespace Tailbook.Modules.Identity.Api.Auth.Login;

public sealed class LoginResponse
{
    public string Status { get; set; } = LoginResponseStatusCodes.Authenticated;
    public string? AccessToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public AuthenticatedUserView? User { get; set; }
    public MfaChallengeResponse? MfaChallenge { get; set; }
}

public sealed class MfaChallengeResponse
{
    public Guid ChallengeId { get; set; }
    public string FactorType { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public static class LoginResponseStatusCodes
{
    public const string Authenticated = "Authenticated";
    public const string MfaRequired = "MfaRequired";
}
