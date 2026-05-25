using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.VisitOperations.IntegrationEvents;

public sealed record VisitCancelledIntegrationEvent(
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    string? ReasonCode) : IIntegrationEventDto
{
    public int EventVersion => VisitOperationsIntegrationEventVersions.VisitCancelled;
}
