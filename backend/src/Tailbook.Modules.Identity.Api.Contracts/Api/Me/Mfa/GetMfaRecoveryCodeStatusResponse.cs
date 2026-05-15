namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class GetMfaRecoveryCodeStatusResponse
{
    public int ActiveCodeCount { get; set; }
    public DateTimeOffset? LastGeneratedAt { get; set; }
}
