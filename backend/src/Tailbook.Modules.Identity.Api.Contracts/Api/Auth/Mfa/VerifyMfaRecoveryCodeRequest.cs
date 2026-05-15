namespace Tailbook.Modules.Identity.Api.Auth.Mfa;

public sealed class VerifyMfaRecoveryCodeRequest
{
    public Guid ChallengeId { get; set; }
    public string RecoveryCode { get; set; } = string.Empty;
    public bool TrustDevice { get; set; }
}
