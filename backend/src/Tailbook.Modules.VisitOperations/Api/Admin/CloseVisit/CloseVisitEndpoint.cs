using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
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
        try
        {
            var result = await visitQueries.CloseVisitAsync(req.VisitId, req.ActorUserId, ct);
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
