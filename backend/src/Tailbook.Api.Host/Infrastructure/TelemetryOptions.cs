namespace Tailbook.Api.Host.Infrastructure;

public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public bool Enabled { get; init; } = true;

    public string ServiceName { get; init; } = "tailbook-api";

    public string DatabasePoolName { get; init; } = "tailbook-main";

    public bool ExportLogs { get; init; } = true;

    public string? OtlpEndpoint { get; init; }

    public bool HasOtlpEndpoint => !string.IsNullOrWhiteSpace(OtlpEndpoint);

    public bool HasExportableOtlpEndpoint => HasOtlpEndpoint && HasValidOtlpEndpoint(this);

    public bool ShouldExportLogs => Enabled && ExportLogs && HasExportableOtlpEndpoint;

    public static bool HasValidServiceName(TelemetryOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.ServiceName);
    }

    public static bool HasValidDatabasePoolName(TelemetryOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.DatabasePoolName);
    }

    public static bool HasValidOtlpEndpoint(TelemetryOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            return true;
        }

        return Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
