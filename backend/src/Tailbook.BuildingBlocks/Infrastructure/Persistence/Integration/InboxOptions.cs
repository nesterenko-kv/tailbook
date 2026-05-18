namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class InboxOptions
{
    public const string SectionName = "Inbox";

    public bool EnableBackgroundProcessing { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 15;
    public int MaxRetryAttempts { get; set; } = 5;
    public int BackoffBaseDelaySeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
}
