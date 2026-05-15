namespace Tailbook.BuildingBlocks.Abstractions;

public sealed record UserScope(string ScopeType, string? ScopeId);

public interface IScopeAuthorizationService
{
    Task<bool> HasGlobalScopeAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<UserScope>> GetUserScopesAsync(Guid userId, CancellationToken cancellationToken);
}
