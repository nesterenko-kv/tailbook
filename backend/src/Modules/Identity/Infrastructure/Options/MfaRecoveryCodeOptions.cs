namespace Tailbook.Modules.Identity.Infrastructure.Options;

public sealed class MfaRecoveryCodeOptions
{
    public const string SectionName = "MfaRecoveryCodes";

    public int CodeCount { get; set; } = 10;
    public int CodeLength { get; set; } = 16;
}
