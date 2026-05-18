namespace Tailbook.BuildingBlocks.Abstractions;

public interface IInboxStore
{
    Task<bool> TryReceiveAsync(string messageId, string consumerName, string eventType, string payloadJson, CancellationToken ct);
}
