namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string ConsumerName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = "Received";
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public bool IsPoisoned { get; set; }
    public DateTimeOffset? PoisonedAt { get; set; }
}
