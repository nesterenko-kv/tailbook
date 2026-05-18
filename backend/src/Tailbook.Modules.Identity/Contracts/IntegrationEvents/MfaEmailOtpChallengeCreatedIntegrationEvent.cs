using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Identity.Contracts.IntegrationEvents;

public sealed record MfaEmailOtpChallengeCreatedIntegrationEvent(
    string Email,
    string DisplayName,
    Guid ChallengeId,
    string ProtectedCode,
    DateTimeOffset ExpiresAt) : IIntegrationEventDto
{
    public int EventVersion => IdentityIntegrationEventVersions.MfaEmailOtpChallengeCreated;
}
