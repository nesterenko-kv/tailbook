using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.VisitOperations.Contracts.IntegrationEvents;

public static class VisitOperationsIntegrationEventVersions
{
    public const int FinalPriceAdjusted = IntegrationEventVersionPolicy.InitialVersion;
    public const int VisitCheckedIn = IntegrationEventVersionPolicy.InitialVersion;
    public const int VisitClosed = IntegrationEventVersionPolicy.InitialVersion;
    public const int VisitCompleted = IntegrationEventVersionPolicy.InitialVersion;
}
