using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CloseVisit;

public sealed class CloseVisitEndpoint(VisitQueries visitQueries)
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
        var result = await visitQueries.CloseVisitAsync(req.VisitId, req.ActorUserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class CloseVisitRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid VisitId { get; set; }
}

public sealed class CloseVisitRequestValidator : Validator<CloseVisitRequest>
{
    public CloseVisitRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
    }
}
