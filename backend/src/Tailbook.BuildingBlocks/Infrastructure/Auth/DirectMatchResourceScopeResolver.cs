using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public sealed class DirectMatchResourceScopeResolver : IResourceScopeResolver
{
    public bool CanResolve(string resourceType, string scopeType)
    {
        return string.Equals(resourceType, scopeType, StringComparison.OrdinalIgnoreCase);
    }

    public ValueTask<bool> IsResourceInScopeAsync(string resourceType, string resourceId, string scopeType, string scopeId, CancellationToken cancellationToken)
    {
        var result = string.Equals(resourceId, scopeId, StringComparison.OrdinalIgnoreCase);
        return ValueTask.FromResult(result);
    }
}
