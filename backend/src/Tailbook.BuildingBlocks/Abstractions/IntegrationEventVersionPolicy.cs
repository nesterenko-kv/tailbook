namespace Tailbook.BuildingBlocks.Abstractions;

public static class IntegrationEventVersionPolicy
{
    // Integration DTO versions start at 1. Keep the current version for additive nullable fields;
    // increment the event-specific constant for renamed, removed, required, or semantic changes.
    public const int InitialVersion = 1;

    public static void EnsureValid(int eventVersion, string eventType)
    {
        if (eventVersion < InitialVersion)
        {
            throw new InvalidOperationException(
                $"Integration event '{eventType}' has invalid eventVersion '{eventVersion}'. Event versions start at {InitialVersion}.");
        }
    }
}
