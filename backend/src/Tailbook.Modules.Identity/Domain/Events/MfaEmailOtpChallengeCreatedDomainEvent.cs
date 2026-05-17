using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Identity.Domain.Events;

public sealed record MfaEmailOtpChallengeCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string Email,
    string DisplayName,
    Guid ChallengeId,
    string ProtectedCode,
    DateTimeOffset ExpiresAt) : IDomainEvent
{
    public string EventType => "Tailbook.Modules.Identity.Integration.MfaEmailOtpChallengeCreated";
    public string ModuleCode => "identity";
}
