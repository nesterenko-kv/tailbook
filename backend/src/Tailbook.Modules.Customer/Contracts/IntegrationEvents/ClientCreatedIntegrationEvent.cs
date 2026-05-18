using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Customer.Contracts.IntegrationEvents;

public sealed record ClientCreatedIntegrationEvent(
    Guid ClientId,
    string DisplayName,
    string Status,
    string? Notes) : IIntegrationEventDto
{
    public int EventVersion => CustomerIntegrationEventVersions.ClientCreated;
}
