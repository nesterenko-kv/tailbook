using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class EnableEmailMfaEndpoint(IMfaFactorService mfaFactorService)
    : Endpoint<EnableEmailMfaRequest, MfaFactorView>
{
    public override void Configure()
    {
        Post("/api/identity/me/mfa/email");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(EnableEmailMfaRequest req, CancellationToken ct)
    {
        var factorResult = await mfaFactorService.EnableEmailOtpAsync(req.UserId, ct);
        if (factorResult.IsError)
        {
            await Send.ResultAsync(factorResult.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(factorResult.Value, cancellation: ct);
    }
}
