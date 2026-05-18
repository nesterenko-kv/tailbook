using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.VisitOperations.Contracts.IntegrationEvents;

namespace Tailbook.Modules.VisitOperations.Domain.Events;

public sealed record FinalPriceAdjustedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VisitId,
    string Status,
    int Sign,
    decimal Amount,
    string ReasonCode) : IDomainEvent
{
    public string EventType => "FinalPriceAdjusted";
    public string ModuleCode => "visitops";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new FinalPriceAdjustedIntegrationEvent(VisitId, Status, Sign, Amount, ReasonCode);
    }
}
