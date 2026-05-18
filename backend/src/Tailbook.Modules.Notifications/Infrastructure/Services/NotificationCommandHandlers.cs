using ErrorOr;
using FastEndpoints;
using Tailbook.Modules.Notifications.Infrastructure.Telemetry;

namespace Tailbook.Modules.Notifications.Infrastructure.Services;

public sealed class NotificationCommandHandlers(NotificationUseCases useCases)
    : ICommandHandler<ProcessNotificationsCommand, int>,
      ICommandHandler<RequeueNotificationJobCommand, ErrorOr<NotificationJobListItemView>>,
      ICommandHandler<AbandonNotificationJobCommand, ErrorOr<NotificationJobListItemView>>
{
    public Task<int> ExecuteAsync(ProcessNotificationsCommand command, CancellationToken ct = default)
    {
        return useCases.ProcessPendingNotificationsAsync(NotificationTelemetry.TriggerManual, ct);
    }

    public Task<ErrorOr<NotificationJobListItemView>> ExecuteAsync(RequeueNotificationJobCommand command, CancellationToken ct = default)
    {
        return useCases.RequeueJobAsync(command.JobId, command.ActorUserId, ct);
    }

    public Task<ErrorOr<NotificationJobListItemView>> ExecuteAsync(AbandonNotificationJobCommand command, CancellationToken ct = default)
    {
        return useCases.AbandonJobAsync(command.JobId, command.ActorUserId, ct);
    }
}
