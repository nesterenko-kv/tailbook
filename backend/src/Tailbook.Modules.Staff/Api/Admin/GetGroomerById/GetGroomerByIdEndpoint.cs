using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerById;

public sealed class GetGroomerByIdEndpoint(
    IStaffReadService staffReadService,
    IEntityScopeService entityScopeService)
    : Endpoint<GetGroomerByIdRequest, CreateGroomerResponse>
{
    public override void Configure()
    {
        Get("/api/admin/groomers/{groomerId:guid}");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.read");
    }

    public override async Task HandleAsync(GetGroomerByIdRequest req, CancellationToken ct)
    {
        var groomer = await staffReadService.GetGroomerAsync(req.GroomerId, ct);
        if (groomer is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.Groomer, req.GroomerId.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(GroomerResponseMapper.ToCreateGroomerResponse(groomer), cancellation: ct);
    }
}