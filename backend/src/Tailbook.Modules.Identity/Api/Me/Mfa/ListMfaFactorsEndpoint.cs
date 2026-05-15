using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class ListMfaFactorsEndpoint(IMfaFactorService mfaFactorService)
    : Endpoint<ListMfaFactorsRequest, ListMfaFactorsResponse>
{
    public override void Configure()
    {
        Get("/api/identity/me/mfa/factors");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(ListMfaFactorsRequest req, CancellationToken ct)
    {
        var factors = await mfaFactorService.ListFactorsAsync(req.UserId, ct);
        await Send.OkAsync(new ListMfaFactorsResponse { Items = factors }, cancellation: ct);
    }
}
