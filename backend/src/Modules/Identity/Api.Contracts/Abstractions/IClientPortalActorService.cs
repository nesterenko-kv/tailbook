using ErrorOr;

namespace Tailbook.Modules.Identity.Api.Contracts.Abstractions;

public interface IClientPortalActorService
{
    Task<ErrorOr<ClientPortalActor>> GetActorAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record ClientPortalActor(
    Guid UserId,
    Guid ClientId,
    Guid ContactPersonId,
    string Email,
    string DisplayName);
