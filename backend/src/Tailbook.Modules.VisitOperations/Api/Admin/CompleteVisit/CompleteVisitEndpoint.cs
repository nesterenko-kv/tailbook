using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

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
        try
        {
            var result = await visitQueries.CompleteVisitAsync(req.VisitId, req.ActorUserId, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
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
