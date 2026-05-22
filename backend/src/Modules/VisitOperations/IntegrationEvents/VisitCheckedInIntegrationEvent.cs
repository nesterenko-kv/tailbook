using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.VisitOperations.IntegrationEvents;

public sealed record VisitCheckedInIntegrationEvent(
    Guid VisitId,
    Guid AppointmentId,
    string Status,
    DateTimeOffset CheckedInAt) : IIntegrationEventDto
{
    public int EventVersion => VisitOperationsIntegrationEventVersions.VisitCheckedIn;
}
