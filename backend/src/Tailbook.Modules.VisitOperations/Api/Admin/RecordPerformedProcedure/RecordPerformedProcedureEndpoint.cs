using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.RecordPerformedProcedure;

public sealed class RecordPerformedProcedureEndpoint(VisitQueries visitQueries)
    : Endpoint<RecordPerformedProcedureRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/performed-procedures");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.write");
    }

    public override async Task HandleAsync(RecordPerformedProcedureRequest req, CancellationToken ct)
    {
        var result = await visitQueries.RecordPerformedProcedureAsync(req.VisitId, req.VisitExecutionItemId, req.ProcedureId, req.Note, req.ActorUserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class RecordPerformedProcedureRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid VisitId { get; set; }
    public Guid VisitExecutionItemId { get; set; }
    public Guid ProcedureId { get; set; }
    public string? Note { get; set; }
}

public sealed class RecordPerformedProcedureRequestValidator : Validator<RecordPerformedProcedureRequest>
{
    public RecordPerformedProcedureRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.VisitExecutionItemId).NotEmpty();
        RuleFor(x => x.ProcedureId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
