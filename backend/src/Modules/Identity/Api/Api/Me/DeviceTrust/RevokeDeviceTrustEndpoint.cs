using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Me.DeviceTrust;

public sealed class RevokeDeviceTrustEndpoint(
    IDeviceTrustService deviceTrustService,
    ICurrentUser currentUser) : Endpoint<RevokeDeviceTrustRequest>
{
    public override void Configure()
    {
        Post("/api/identity/me/device-trusts/{trustId:guid}/revoke");
        Description(x => x.WithTags("Identity"));
        PermissionsAll("app.admin.access", "app.groomer.access");
    }

    public override async Task HandleAsync(RevokeDeviceTrustRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is null || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var result = await deviceTrustService.RevokeTrustAsync(req.TrustId, userId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public sealed class RevokeDeviceTrustRequest
{
    public Guid TrustId { get; set; }
}
