namespace Tailbook.BuildingBlocks.Abstractions;

public interface IInboxMessageHandler
{
    string ConsumerName { get; }
    Task HandleAsync(string eventType, string payloadJson, Guid messageId, CancellationToken ct);
}
