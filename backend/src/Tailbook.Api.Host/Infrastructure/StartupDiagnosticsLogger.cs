namespace Tailbook.Api.Host.Infrastructure;

public static class StartupDiagnosticsLogger
{
    public static void Log(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Tailbook.Startup");
        var configuration = app.Configuration;
        var corsOptions = configuration.GetSection(AppCorsOptions.SectionName).Get<AppCorsOptions>() ?? new AppCorsOptions();
        var transportOptions = configuration.GetSection(HttpTransportOptions.SectionName).Get<HttpTransportOptions>() ?? new HttpTransportOptions();
        var connectionString = configuration.GetConnectionString(DatabaseConnectionOptions.MainConnectionStringName);

        logger.LogInformation(
            "Startup diagnostics environment={Environment} databaseHost={DatabaseHost} databaseName={DatabaseName} corsOriginCount={CorsOriginCount} httpsRedirection={HttpsRedirection} hsts={Hsts} notificationsBackground={NotificationsBackground} notificationsPollSeconds={NotificationsPollSeconds} staffTimeZone={StaffTimeZone}",
            app.Environment.EnvironmentName,
            ReadConnectionStringValue(connectionString, "Host") ?? "<unconfigured>",
            ReadConnectionStringValue(connectionString, "Database") ?? "<unconfigured>",
            corsOptions.AllowedOrigins.Length,
            transportOptions.EnforceHttpsRedirection,
            transportOptions.UseHsts,
            configuration.GetValue<bool>("Notifications:EnableBackgroundProcessing"),
            configuration.GetValue<int?>("Notifications:BackgroundPollIntervalSeconds") ?? 0,
            configuration["StaffScheduling:TimeZoneId"] ?? "<unconfigured>");
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
