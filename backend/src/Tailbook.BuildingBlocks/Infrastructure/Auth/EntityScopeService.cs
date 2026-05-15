using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public sealed class EntityScopeService(
    IAccessAuditService accessAuditService,
    IScopeAuthorizationService scopeAuthorizationService,
    IEnumerable<IResourceScopeResolver> scopeResolvers) : IEntityScopeService
{
    public async Task<ErrorOr<Success>> VerifyAccessAsync(string resourceType, string resourceId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        await accessAuditService.RecordAsync(resourceType, resourceId, "READ_DETAIL", actorUserId, cancellationToken);

        if (actorUserId is null)
            return Error.Forbidden("Scope.NoActor", "Authenticated user required for scope check.");

        var hasGlobalScope = await scopeAuthorizationService.HasGlobalScopeAsync(actorUserId.Value, cancellationToken);
        if (hasGlobalScope)
            return Result.Success;

        var userScopes = await scopeAuthorizationService.GetUserScopesAsync(actorUserId.Value, cancellationToken);
        foreach (var userScope in userScopes)
        {
            foreach (var resolver in scopeResolvers)
            {
                if (!resolver.CanResolve(resourceType, userScope.ScopeType))
                    continue;

                var inScope = await resolver.IsResourceInScopeAsync(resourceType, resourceId, userScope.ScopeType, userScope.ScopeId ?? string.Empty, cancellationToken);
                if (inScope)
                    return Result.Success;
            }
        }

        return Error.Forbidden("Scope.Denied", "User does not have scope access to this resource.");
    }
}

public static class EntityScopeResourceTypes
{
    public const string Client = "client";
    public const string Pet = "pet";
    public const string Appointment = "appointment";
    public const string BookingRequest = "booking_request";
    public const string Groomer = "groomer";
    public const string IamUser = "iam_user";
    public const string Visit = "visit";
    public const string Offer = "offer";
    public const string NotificationJob = "notification_job";
}
