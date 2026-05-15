namespace Tailbook.Modules.Identity.Api.Admin.ResetMfaRecovery;

public sealed class ResetMfaRecoveryResponse
{
    public Guid UserId { get; set; }
    public int DisabledFactorCount { get; set; }
    public int InvalidatedRecoveryCodeCount { get; set; }
    public int InvalidatedChallengeCount { get; set; }
    public DateTimeOffset ResetAt { get; set; }
}
