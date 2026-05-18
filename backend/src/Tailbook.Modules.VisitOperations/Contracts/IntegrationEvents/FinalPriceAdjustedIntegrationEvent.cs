using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.VisitOperations.Contracts.IntegrationEvents;

public sealed record FinalPriceAdjustedIntegrationEvent(
    Guid VisitId,
    string Status,
    int Sign,
    decimal Amount,
    string ReasonCode) : IIntegrationEventDto
{
    public int EventVersion => VisitOperationsIntegrationEventVersions.FinalPriceAdjusted;
}
