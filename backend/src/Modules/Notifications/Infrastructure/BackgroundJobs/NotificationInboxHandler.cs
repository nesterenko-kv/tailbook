using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Notifications.Infrastructure.Services;

namespace Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;

public sealed class NotificationInboxHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationInboxHandler> logger) : IInboxMessageHandler
{
    public string ConsumerName => "notifications";

    public async Task HandleAsync(string eventType, string payloadJson, Guid messageId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var useCases = scope.ServiceProvider.GetRequiredService<NotificationUseCases>();

        var result = await useCases.ProcessBrokerNotificationAsync(
            eventType,
            payloadJson,
            messageId,
            ct);

        if (result.Outcome == "sent")
        {
            logger.NotificationDispatchedFromIntegrationEvent(messageId, eventType);
        }
        else if (result.Outcome == "dead_letter")
        {
            logger.LogWarning("Notification from integration event {MessageId} ({EventType}) dead-lettered: {Error}", messageId, eventType, result.ErrorMessage);
        }
        else if (result.Outcome == "ignored")
        {
            logger.LogDebug("Integration event {MessageId} ({EventType}) ignored for notifications (no matching template).", messageId, eventType);
        }
    }
}
