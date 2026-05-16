using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class GetMfaRecoveryCodeStatusEndpoint(IMfaRecoveryCodeService recoveryCodeService)
    : Endpoint<GetMfaRecoveryCodeStatusRequest, GetMfaRecoveryCodeStatusResponse>
{
    public override void Configure()
    {
        Get("/api/identity/me/mfa/recovery-codes");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(GetMfaRecoveryCodeStatusRequest req, CancellationToken ct)
    {
        var status = await recoveryCodeService.GetRecoveryCodeStatusAsync(req.UserId, ct);
        await Send.OkAsync(new GetMfaRecoveryCodeStatusResponse
        {
            ActiveCodeCount = status.ActiveCodeCount,
            LastGeneratedAt = status.LastGeneratedAt
        }, cancellation: ct);
    }
}
