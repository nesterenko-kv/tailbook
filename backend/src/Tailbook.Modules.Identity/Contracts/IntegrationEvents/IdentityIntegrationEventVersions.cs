using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Identity.Contracts.IntegrationEvents;

public static class IdentityIntegrationEventVersions
{
    public const int MfaEmailOtpChallengeCreated = IntegrationEventVersionPolicy.InitialVersion;
    public const int PasswordResetRequested = IntegrationEventVersionPolicy.InitialVersion;
}
