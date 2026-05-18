namespace Tailbook.Api.Host.Infrastructure;

public static class StartupDiagnosticsLogger
{
    public static void Log(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Tailbook.Startup");
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var configuration = app.Configuration;
        var corsOptions = configuration.GetSection(AppCorsOptions.SectionName).Get<AppCorsOptions>() ?? new AppCorsOptions();
        var transportOptions = configuration.GetSection(HttpTransportOptions.SectionName).Get<HttpTransportOptions>() ?? new HttpTransportOptions();
        var telemetryOptions = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>() ?? new TelemetryOptions();
        var connectionString = configuration.GetConnectionString(DatabaseConnectionOptions.MainConnectionStringName);
        var environmentName = app.Environment.EnvironmentName;
        var databaseHost = ReadConnectionStringValue(connectionString, "Host") ?? "<unconfigured>";
        var databaseName = ReadConnectionStringValue(connectionString, "Database") ?? "<unconfigured>";
        var corsOriginCount = corsOptions.AllowedOrigins.Length;
        var httpsRedirectionEnabled = transportOptions.EnforceHttpsRedirection;
        var hstsEnabled = transportOptions.UseHsts;
        var notificationsBackgroundEnabled = configuration.GetValue<bool>("Notifications:EnableBackgroundProcessing");
        var integrationOutboxPublishingEnabled = configuration.GetValue<bool>("IntegrationOutbox:EnableBackgroundPublishing");
        var integrationOutboxPollSeconds = configuration.GetValue<int?>("IntegrationOutbox:PollIntervalSeconds") ?? 0;
        var staffTimeZoneId = configuration["StaffScheduling:TimeZoneId"] ?? "<unconfigured>";
        var telemetryEnabled = telemetryOptions.Enabled;
        var otlpExportConfigured = telemetryOptions.HasOtlpEndpoint;
        var otlpLogExportConfigured = telemetryOptions.ShouldExportLogs;
        var databasePoolName = telemetryOptions.DatabasePoolName;

        logger.StartupDiagnostics(
            environmentName,
            databaseHost,
            databaseName,
            corsOriginCount,
            httpsRedirectionEnabled,
            hstsEnabled,
            notificationsBackgroundEnabled,
            integrationOutboxPublishingEnabled,
            integrationOutboxPollSeconds,
            staffTimeZoneId,
            telemetryEnabled,
            otlpExportConfigured,
            otlpLogExportConfigured,
            databasePoolName
            );
    }

    private static string? ReadConnectionStringValue(string? connectionString, string key)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = segment[..separatorIndex].Trim();
            if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = segment[(separatorIndex + 1)..].Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}

internal static partial class StartupDiagnosticsMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Startup diagnostics: environment {EnvironmentName}, database host {DatabaseHost}, database name {DatabaseName}, CORS origin count {CorsOriginCount}, HTTPS redirection {HttpsRedirectionEnabled}, HSTS {HstsEnabled}, notifications background {NotificationsBackgroundEnabled}, integration outbox publishing {IntegrationOutboxPublishingEnabled}, integration outbox poll {IntegrationOutboxPollSeconds}s, staff time zone {StaffTimeZoneId}, telemetry enabled {TelemetryEnabled}, OTLP export configured {OtlpExportConfigured}, OTLP log export configured {OtlpLogExportConfigured}, database pool {DatabasePoolName}")]
    public static partial void StartupDiagnostics(
        this ILogger logger,
        string environmentName,
        string databaseHost,
        string databaseName,
        int corsOriginCount,
        bool httpsRedirectionEnabled,
        bool hstsEnabled,
        bool notificationsBackgroundEnabled,
        bool integrationOutboxPublishingEnabled,
        int integrationOutboxPollSeconds,
        string staffTimeZoneId,
        bool telemetryEnabled,
        bool otlpExportConfigured,
        bool otlpLogExportConfigured,
        string databasePoolName);
}
