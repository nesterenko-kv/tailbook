namespace Tailbook.Modules.Notifications.Infrastructure.Options;

public sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    public bool EnableBackgroundProcessing { get; set; }
    public int BackgroundPollIntervalSeconds { get; set; } = 15;
    public string LocalFilePath { get; set; } = "./data/notifications/notifications.log";
}
