using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class ListMfaFactorsEndpoint(MfaFactorService mfaFactorService)
    : Endpoint<ListMfaFactorsRequest, ListMfaFactorsResponse>
{
    public override void Configure()
    {
        Get("/api/identity/me/mfa/factors");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(ListMfaFactorsRequest req, CancellationToken ct)
    {
        if (req.UserId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var factors = await mfaFactorService.ListFactorsAsync(req.UserId.Value, ct);
        await Send.OkAsync(new ListMfaFactorsResponse { Items = factors }, cancellation: ct);
    }
}

public sealed class ListMfaFactorsRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? UserId { get; set; }
}

public sealed class ListMfaFactorsResponse
{
    public IReadOnlyCollection<MfaFactorView> Items { get; set; } = [];
}
