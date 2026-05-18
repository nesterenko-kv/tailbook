using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Identity.Contracts.IntegrationEvents;

public sealed record PasswordResetRequestedIntegrationEvent(
    string Email,
    string DisplayName,
    string ProtectedResetLink,
    DateTimeOffset ExpiresAt) : IIntegrationEventDto
{
    public int EventVersion => IdentityIntegrationEventVersions.PasswordResetRequested;
}
