namespace Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

internal sealed class AuditWriteOptions
{
    public const string SectionName = "Audit";

    public int QueueCapacity { get; init; } = 10_000;
    public int BatchSize { get; init; } = 100;
    public int FlushIntervalMilliseconds { get; init; } = 1_000;
    public int MaxWriteRetries { get; init; } = 3;
    public int RetryDelayMilliseconds { get; init; } = 100;

    public TimeSpan FlushInterval => TimeSpan.FromMilliseconds(FlushIntervalMilliseconds);
    public TimeSpan RetryDelay => TimeSpan.FromMilliseconds(RetryDelayMilliseconds);
}
