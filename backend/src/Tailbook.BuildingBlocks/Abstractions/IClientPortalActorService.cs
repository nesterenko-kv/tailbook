namespace Tailbook.BuildingBlocks.Abstractions;

public interface IClientPortalActorService
{
    Task<ClientPortalActor?> GetActorAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record ClientPortalActor(
    Guid UserId,
    Guid ClientId,
    Guid ContactPersonId,
    string Email,
    string DisplayName);
