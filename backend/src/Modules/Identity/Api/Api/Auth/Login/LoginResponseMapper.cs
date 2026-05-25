namespace Tailbook.Modules.Identity.Api.Auth.Login;

internal static class LoginResponseMapper
{
    public static LoginResponse FromLoginResult(LoginResult login, bool includeRefreshToken = true) => new LoginResponse
    {
        Status = LoginResponseStatusCodes.Authenticated,
        AccessToken = login.AccessToken,
        ExpiresAt = login.ExpiresAt,
        RefreshToken = includeRefreshToken ? login.RefreshToken : null,
        RefreshTokenExpiresAt = login.RefreshTokenExpiresAt,
        User = login.User
    };

    public static LoginResponse FromMfaChallenge(AuthenticationMfaRequiredResult challenge) => new LoginResponse
    {
        Status = LoginResponseStatusCodes.MfaRequired,
        MfaChallenge = new MfaChallengeResponse
        {
            ChallengeId = challenge.ChallengeId,
            FactorType = challenge.FactorType,
            ExpiresAt = challenge.ExpiresAt
        }
    };
}
