using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class EnableEmailMfaEndpoint(MfaFactorService mfaFactorService)
    : Endpoint<EnableEmailMfaRequest, MfaFactorView>
{
    public override void Configure()
    {
        Post("/api/identity/me/mfa/email");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(EnableEmailMfaRequest req, CancellationToken ct)
    {
        if (req.UserId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var factor = await mfaFactorService.EnableEmailOtpAsync(req.UserId.Value, ct);
        if (factor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(factor, cancellation: ct);
    }
}

public sealed class EnableEmailMfaRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? UserId { get; set; }
}
