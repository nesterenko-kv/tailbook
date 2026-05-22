namespace Tailbook.Modules.Identity.Infrastructure.Options;

public sealed class DeviceTrustOptions
{
    public const string SectionName = "DeviceTrust";

    public int DurationDays { get; set; } = 30;
    public int TokenBytes { get; set; } = 32;
}
