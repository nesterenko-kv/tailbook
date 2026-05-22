using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.GetVisitDetail;

public sealed class GetVisitDetailEndpoint(
    IVisitReadService visitReadService,
    IEntityScopeService entityScopeService)
    : Endpoint<GetVisitDetailRequest, VisitDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/visits/{visitId:guid}");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.read");
    }

    public override async Task HandleAsync(GetVisitDetailRequest req, CancellationToken ct)
    {
        var result = await visitReadService.GetVisitAsync(req.VisitId, req.ActorUserId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.Visit, req.VisitId.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result, ct);
    }
}