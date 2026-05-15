using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CloseVisit;

public sealed class CloseVisitEndpoint(
    IEntityScopeService entityScopeService)
    : Endpoint<CloseVisitRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/close");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.write");
    }

    public override async Task HandleAsync(CloseVisitRequest req, CancellationToken ct)
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

        var command = new CloseVisitUseCaseCommand(req.VisitId, req.ActorUserId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}