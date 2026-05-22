using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Notifications.Application.Notifications.Commands;

public sealed record RequeueNotificationJobCommand(Guid JobId, Guid ActorUserId) : ICommand<ErrorOr<NotificationJobListItemView>>;
