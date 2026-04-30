using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Admin.RecordSkippedComponent;

public sealed class RecordSkippedComponentEndpoint(VisitQueries visitQueries)
    : Endpoint<RecordSkippedComponentRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/skipped-components");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.write");
    }

    public override async Task HandleAsync(RecordSkippedComponentRequest req, CancellationToken ct)
    {
        var result = await visitQueries.RecordSkippedComponentAsync(req.VisitId, req.VisitExecutionItemId, req.OfferVersionComponentId, req.OmissionReasonCode, req.Note, req.ActorUserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class RecordSkippedComponentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid VisitId { get; set; }
    public Guid VisitExecutionItemId { get; set; }
    public Guid OfferVersionComponentId { get; set; }
    public string OmissionReasonCode { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class RecordSkippedComponentRequestValidator : Validator<RecordSkippedComponentRequest>
{
    public RecordSkippedComponentRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.VisitExecutionItemId).NotEmpty();
        RuleFor(x => x.OfferVersionComponentId).NotEmpty();
        RuleFor(x => x.OmissionReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
