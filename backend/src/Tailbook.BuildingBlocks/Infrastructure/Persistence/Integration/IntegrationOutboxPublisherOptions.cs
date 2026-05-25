namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class IntegrationOutboxPublisherOptions
{
    public const string SectionName = "IntegrationOutbox";

    public bool EnableBackgroundPublishing { get; set; }
    public int PollIntervalSeconds { get; set; } = 15;
    public int MaxRetryAttempts { get; set; } = 5;
    public int BackoffBaseDelaySeconds { get; set; } = 5;
}
