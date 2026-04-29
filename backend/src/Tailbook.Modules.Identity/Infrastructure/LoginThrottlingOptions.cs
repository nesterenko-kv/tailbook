namespace Tailbook.Modules.Identity.Infrastructure;

public sealed class LoginThrottlingOptions
{
    public const string SectionName = "LoginThrottling";

    public int MaxFailedAttempts { get; set; } = 5;
    public int FailureWindowMinutes { get; set; } = 15;
    public int LockoutMinutes { get; set; } = 15;
}
