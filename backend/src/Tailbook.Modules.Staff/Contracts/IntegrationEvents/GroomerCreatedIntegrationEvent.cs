using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Staff.Contracts.IntegrationEvents;

public sealed record GroomerCreatedIntegrationEvent(
    Guid GroomerId,
    Guid? UserId,
    string DisplayName,
    bool Active) : IIntegrationEventDto
{
    public int EventVersion => StaffIntegrationEventVersions.GroomerCreated;
}
