namespace Tailbook.Modules.Notifications.Infrastructure.Services;

public sealed class NotificationDeliveryException(string message, Exception innerException)
    : Exception(message, innerException);
