using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.VisitOperations.Contracts.IntegrationEvents;

public sealed record VisitClosedIntegrationEvent(
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    decimal FinalTotalAmount,
    DateTimeOffset ClosedAt) : IIntegrationEventDto
{
    public int EventVersion => VisitOperationsIntegrationEventVersions.VisitClosed;
}
