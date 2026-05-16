using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me.DeviceTrust;

public sealed class ListDeviceTrustsEndpoint(
    IDeviceTrustService deviceTrustService,
    ICurrentUser currentUser) : EndpointWithoutRequest<DeviceTrustListResponse>
{
    public override void Configure()
    {
        Get("/api/identity/me/device-trusts");
        Description(x => x.WithTags("Identity"));
        PermissionsAll("app.admin.access", "app.groomer.access");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (currentUser.UserId is null || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var trusts = await deviceTrustService.ListTrustsAsync(userId, ct);
        await Send.OkAsync(new DeviceTrustListResponse { Items = trusts }, ct);
    }
}

public sealed class DeviceTrustListResponse
{
    public IReadOnlyCollection<DeviceTrustView> Items { get; set; } = [];
}
