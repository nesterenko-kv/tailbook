using Microsoft.Extensions.Logging;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

internal static partial class IntegrationEventConsumerBaseLogExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Consumer started. Queue: {Queue}, Exchange: {Exchange}, Bindings: {BindingCount}.")]
    public static partial void ConsumerStarted(this ILogger logger, string queue, string exchange, int bindingCount);

    [LoggerMessage(2, LogLevel.Information, "Consumer stopped. Queue: {Queue}.")]
    public static partial void ConsumerStopped(this ILogger logger, string queue);
}
