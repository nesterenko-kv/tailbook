using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Staff.IntegrationEvents;

public sealed record GroomerCreatedIntegrationEvent(
    Guid GroomerId,
    Guid? UserId,
    string DisplayName,
    bool Active) : IIntegrationEventDto
{
    public int EventVersion => StaffIntegrationEventVersions.GroomerCreated;
}
