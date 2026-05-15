using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public sealed class DirectMatchResourceScopeResolver : IResourceScopeResolver
{
    public bool CanResolve(string resourceType, string scopeType)
    {
        return string.Equals(resourceType, scopeType, StringComparison.OrdinalIgnoreCase);
    }

    public Task<bool> IsResourceInScopeAsync(string resourceType, string resourceId, string scopeType, string scopeId, CancellationToken cancellationToken)
    {
        var result = string.Equals(resourceId, scopeId, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(result);
    }
}
