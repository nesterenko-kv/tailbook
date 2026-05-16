namespace Tailbook.BuildingBlocks.Abstractions;

public interface IResourceScopeResolver
{
    bool CanResolve(string resourceType, string scopeType);
    ValueTask<bool> IsResourceInScopeAsync(string resourceType, string resourceId, string scopeType, string scopeId, CancellationToken cancellationToken);
}
