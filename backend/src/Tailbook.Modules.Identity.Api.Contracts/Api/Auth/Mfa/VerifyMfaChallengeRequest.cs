namespace Tailbook.Modules.Identity.Api.Auth.Mfa;

public sealed class VerifyMfaChallengeRequest
{
    public Guid ChallengeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool TrustDevice { get; set; }
}
