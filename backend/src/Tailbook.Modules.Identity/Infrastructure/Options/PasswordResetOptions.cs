namespace Tailbook.Modules.Identity.Infrastructure.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int ExpirationMinutes { get; set; } = 30;
    public int TokenBytes { get; set; } = 32;
}
