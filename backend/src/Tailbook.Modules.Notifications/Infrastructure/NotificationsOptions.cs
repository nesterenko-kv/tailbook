namespace Tailbook.Modules.Notifications.Infrastructure;

public sealed class NotificationsOptions
{
    public bool EnableBackgroundProcessing { get; set; }
    public string LocalFilePath { get; set; } = "./data/notifications/dev-notifications.log";
}
