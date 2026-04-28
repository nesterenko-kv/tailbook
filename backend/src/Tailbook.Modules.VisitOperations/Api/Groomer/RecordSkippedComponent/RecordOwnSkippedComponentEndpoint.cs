using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.RecordSkippedComponent;

public sealed class RecordOwnSkippedComponentEndpoint(
    ICurrentUser currentUser,
    IGroomerVisitAccessPolicy accessPolicy,
    GroomerVisitQueries groomerVisitQueries)
    : Endpoint<RecordOwnSkippedComponentRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Post("/api/groomer/visits/{visitId:guid}/skipped-components");
        Description(x => x.WithTags("Groomer Visits"));
    }

    public override async Task HandleAsync(RecordOwnSkippedComponentRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanWriteOwnVisits(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await groomerVisitQueries.RecordSkippedComponentAsync(currentUserId, req.VisitId, req.VisitExecutionItemId, req.OfferVersionComponentId, req.OmissionReasonCode, req.Note, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class RecordOwnSkippedComponentRequest
{
    public Guid VisitId { get; set; }
    public Guid VisitExecutionItemId { get; set; }
    public Guid OfferVersionComponentId { get; set; }
    public string OmissionReasonCode { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class RecordOwnSkippedComponentRequestValidator : Validator<RecordOwnSkippedComponentRequest>
{
    public RecordOwnSkippedComponentRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.VisitExecutionItemId).NotEmpty();
        RuleFor(x => x.OfferVersionComponentId).NotEmpty();
        RuleFor(x => x.OmissionReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
