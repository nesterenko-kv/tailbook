using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Identity.Contracts.IntegrationEvents;

namespace Tailbook.Modules.Identity.Domain.Events;

public sealed record PasswordResetRequestedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string Email,
    string DisplayName,
    string ProtectedResetLink,
    DateTimeOffset ExpiresAt) : IDomainEvent
{
    public string EventType => "Tailbook.Modules.Identity.Integration.PasswordResetRequested";
    public string ModuleCode => "identity";

    public IIntegrationEventDto ToIntegrationEvent()
    {
        return new PasswordResetRequestedIntegrationEvent(Email, DisplayName, ProtectedResetLink, ExpiresAt);
    }
}
