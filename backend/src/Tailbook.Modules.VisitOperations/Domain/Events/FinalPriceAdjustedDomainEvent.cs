using Tailbook.BuildingBlocks.Abstractions;

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
}
