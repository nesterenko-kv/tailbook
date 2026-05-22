namespace Tailbook.Modules.Identity.Infrastructure.Options;

public sealed class MfaChallengeOptions
{
    public const string SectionName = "MfaChallenge";

    public int ExpirationMinutes { get; set; } = 10;
    public int CodeLength { get; set; } = 6;
    public int MaxFailedAttempts { get; set; } = 5;
}
