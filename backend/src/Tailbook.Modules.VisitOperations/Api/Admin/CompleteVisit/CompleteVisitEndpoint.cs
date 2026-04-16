using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CompleteVisit;

public sealed class CompleteVisitEndpoint(ICurrentUser currentUser, IVisitOperationsAccessPolicy accessPolicy, VisitQueries visitQueries)
    : Endpoint<CompleteVisitRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/complete");
        Description(x => x.WithTags("Admin Visits"));
    }

    public override async Task HandleAsync(CompleteVisitRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteVisits(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var actorUserId = Guid.TryParse(currentUser.UserId, out var parsed) ? parsed : (Guid?)null;
            var result = await visitQueries.CompleteVisitAsync(req.VisitId, actorUserId, ct);
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
    public Guid VisitId { get; set; }
}

public sealed class CompleteVisitRequestValidator : Validator<CompleteVisitRequest>
{
    public CompleteVisitRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
    }
}
