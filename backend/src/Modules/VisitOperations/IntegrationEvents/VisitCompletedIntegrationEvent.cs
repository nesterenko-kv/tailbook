using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.VisitOperations.IntegrationEvents;

public sealed record VisitCompletedIntegrationEvent(
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    DateTimeOffset CompletedAt) : IIntegrationEventDto
{
    public int EventVersion => VisitOperationsIntegrationEventVersions.VisitCompleted;
}
