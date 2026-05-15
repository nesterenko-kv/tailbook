namespace Tailbook.BuildingBlocks.Infrastructure.Security;

public sealed class SensitivePayloadProtectionOptions
{
    public const string SectionName = "SensitivePayloadProtection";

    public string Key { get; set; } = string.Empty;
    public string? PreviousKey { get; set; }
}
