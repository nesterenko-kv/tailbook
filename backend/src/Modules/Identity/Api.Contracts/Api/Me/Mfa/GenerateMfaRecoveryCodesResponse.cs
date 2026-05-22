namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class GenerateMfaRecoveryCodesResponse
{
    public IReadOnlyCollection<string> RecoveryCodes { get; set; } = [];
    public int ActiveCodeCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
