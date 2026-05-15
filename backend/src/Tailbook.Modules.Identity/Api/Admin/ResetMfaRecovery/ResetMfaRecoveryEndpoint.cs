using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Admin.ResetMfaRecovery;

public sealed class ResetMfaRecoveryEndpoint(
    IMfaRecoveryCodeService mfaRecoveryCodeService,
    ICurrentUser currentUser)
    : EndpointWithoutRequest<ResetMfaRecoveryResponse>
{
    public override void Configure()
    {
        Post("/api/admin/iam/users/{id:guid}/mfa/recovery/reset");
        Description(x => x.WithTags("Admin IAM"));
        PermissionsAll(PermissionCodes.IamMfaRecoveryWrite);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.UserId, out var actorUserId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var userId = Guid.Parse(HttpContext.Request.RouteValues["id"]!.ToString()!);
        var result = await mfaRecoveryCodeService.ResetMfaRecoveryAsync(userId, actorUserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(new ResetMfaRecoveryResponse
        {
            UserId = result.Value.UserId,
            DisabledFactorCount = result.Value.DisabledFactorCount,
            InvalidatedRecoveryCodeCount = result.Value.InvalidatedRecoveryCodeCount,
            InvalidatedChallengeCount = result.Value.InvalidatedChallengeCount,
            ResetAt = result.Value.ResetAt
        }, cancellation: ct);
    }
}
