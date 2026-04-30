using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.RecordSkippedComponent;

public sealed class RecordOwnSkippedComponentEndpoint()
    : Endpoint<RecordOwnSkippedComponentRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Post("/api/groomer/visits/{visitId:guid}/skipped-components");
        Description(x => x.WithTags("Groomer Visits"));
        PermissionsAll("app.groomer.access", "groomer.visits.write");
    }

    public override async Task HandleAsync(RecordOwnSkippedComponentRequest req, CancellationToken ct)
    {
        var result = await new RecordOwnSkippedComponentUseCaseCommand(req.UserId, req.VisitId, req.VisitExecutionItemId, req.OfferVersionComponentId, req.OmissionReasonCode, req.Note).ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class RecordOwnSkippedComponentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

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
