namespace Tailbook.BuildingBlocks.Abstractions;

public static class ScopeFilter
{
    public static async Task<IReadOnlyCollection<T>> ApplyAsync<T>(
        IReadOnlyCollection<T> items,
        Guid actorUserId,
        string resourceType,
        Func<T, string> getResourceId,
        IScopeAuthorizationService scopeAuthorizationService,
        CancellationToken cancellationToken)
    {
        var hasGlobal = await scopeAuthorizationService.HasGlobalScopeAsync(actorUserId, cancellationToken);
        if (hasGlobal)
            return items;

        var userScopes = await scopeAuthorizationService.GetUserScopesAsync(actorUserId, cancellationToken);
        var allowedIds = userScopes
            .Where(s => string.Equals(s.ScopeType, resourceType, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.ScopeId)
            .Where(id => id is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowedIds.Count == 0)
            return Array.Empty<T>();

        return items.Where(item => allowedIds.Contains(getResourceId(item))).ToArray();
    }
}
