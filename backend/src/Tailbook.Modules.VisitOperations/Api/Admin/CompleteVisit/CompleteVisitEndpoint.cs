using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CompleteVisit;

public sealed class CompleteVisitEndpoint(VisitQueries visitQueries)
    : Endpoint<CompleteVisitRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/complete");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.write");
    }

    public override async Task HandleAsync(CompleteVisitRequest req, CancellationToken ct)
    {
        var result = await visitQueries.CompleteVisitAsync(req.VisitId, req.ActorUserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class CompleteVisitRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid VisitId { get; set; }
}

public sealed class CompleteVisitRequestValidator : Validator<CompleteVisitRequest>
{
    public CompleteVisitRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
    }
}
