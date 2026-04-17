namespace Tailbook.Modules.Notifications.Application;

public sealed class LocalNotificationOptions
{
    public const string SectionName = "Notifications";
    public string LocalSinkPath { get; set; } = "ops/local-notifications.log";
}
