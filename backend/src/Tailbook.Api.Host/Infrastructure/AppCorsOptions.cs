namespace Tailbook.Api.Host.Infrastructure;

public sealed class AppCorsOptions
{
    public const string SectionName = "AppCors";

    public string[] AllowedOrigins { get; set; } = [];
}
