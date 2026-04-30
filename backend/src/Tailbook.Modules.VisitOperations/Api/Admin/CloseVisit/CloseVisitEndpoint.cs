using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CloseVisit;

public sealed class CloseVisitEndpoint()
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
        var result = await new CloseVisitUseCaseCommand(req.VisitId, req.ActorUserId).ExecuteAsync(ct);
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
