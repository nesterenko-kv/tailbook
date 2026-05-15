using FastEndpoints;

namespace Tailbook.Modules.Notifications.Application.Notifications.Commands;

public sealed record ProcessNotificationOutboxCommand : ICommand<int>;
