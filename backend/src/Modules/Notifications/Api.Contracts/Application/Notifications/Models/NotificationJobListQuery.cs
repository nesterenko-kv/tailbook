namespace Tailbook.Modules.Notifications.Application.Notifications.Models;

public sealed record NotificationJobListQuery(
    string? Status,
    string? EventType,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo);
