using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.ApplyVisitAdjustment;

public sealed class ApplyVisitAdjustmentEndpoint(
    IEntityScopeService entityScopeService)
    : Endpoint<ApplyVisitAdjustmentRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/adjustments");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll(PermissionCodes.VisitAdjustmentsWrite);
    }

    public override async Task HandleAsync(ApplyVisitAdjustmentRequest req, CancellationToken ct)
    {
        var scopeResult = await entityScopeService.VerifyAccessAsync(
            EntityScopeResourceTypes.Visit,
            req.VisitId.ToString("D"),
            req.ActorUserId,
            ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        var command = new ApplyVisitPriceAdjustmentUseCaseCommand(req.VisitId, req.Sign, req.Amount, req.ReasonCode, req.Note, req.TargetItemId, req.ActorUserId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}