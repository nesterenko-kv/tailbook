using FastEndpoints;

namespace Tailbook.Modules.Notifications.Infrastructure.Services;

public sealed class NotificationCommandHandlers(NotificationUseCases useCases)
    : ICommandHandler<ProcessNotificationOutboxCommand, int>
{
    public Task<int> ExecuteAsync(ProcessNotificationOutboxCommand command, CancellationToken ct = default)
    {
        return useCases.ProcessOutboxAsync(ct);
    }
}
