namespace Tailbook.Api.Host.Infrastructure;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public List<RateLimitRule> Rules { get; set; } = [];
}

public sealed class RateLimitRule
{
    public string PathPrefix { get; set; } = string.Empty;
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}
