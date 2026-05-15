using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class GenerateMfaRecoveryCodesEndpoint(IMfaRecoveryCodeService recoveryCodeService)
    : Endpoint<GenerateMfaRecoveryCodesRequest, GenerateMfaRecoveryCodesResponse>
{
    public override void Configure()
    {
        Post("/api/identity/me/mfa/recovery-codes");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(GenerateMfaRecoveryCodesRequest req, CancellationToken ct)
    {
        var result = await recoveryCodeService.GenerateRecoveryCodesAsync(req.UserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(new GenerateMfaRecoveryCodesResponse
        {
            RecoveryCodes = result.Value.RecoveryCodes,
            ActiveCodeCount = result.Value.ActiveCodeCount,
            CreatedAt = result.Value.CreatedAt
        }, cancellation: ct);
    }
}
