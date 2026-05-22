namespace Tailbook.Modules.Notifications.Infrastructure.Options;

public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";
    public const string LocalFileProvider = "LocalFile";
    public const string SmtpProvider = "Smtp";

    public bool EnableBackgroundProcessing { get; set; }
    public string Provider { get; set; } = LocalFileProvider;
    public int MaxDeliveryAttempts { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 60;
    public int RetryMaxDelaySeconds { get; set; } = 3600;
    public string LocalFilePath { get; set; } = "./data/notifications/notifications.log";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpEnableSsl { get; set; } = true;
    public string SmtpFromEmail { get; set; } = string.Empty;
    public string SmtpFromName { get; set; } = "Tailbook";
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public int SmtpTimeoutSeconds { get; set; } = 30;

    public static bool IsLocalFileProvider(string? provider)
    {
        return string.Equals(provider, LocalFileProvider, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSmtpProvider(string? provider)
    {
        return string.Equals(provider, SmtpProvider, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSupportedProvider(string? provider)
    {
        return IsLocalFileProvider(provider) || IsSmtpProvider(provider);
    }
}
