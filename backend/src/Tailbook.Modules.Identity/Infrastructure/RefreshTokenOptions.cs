namespace Tailbook.Modules.Identity.Infrastructure;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshTokens";

    public int ExpirationDays { get; set; } = 30;
    public int TokenBytes { get; set; } = 32;
}
