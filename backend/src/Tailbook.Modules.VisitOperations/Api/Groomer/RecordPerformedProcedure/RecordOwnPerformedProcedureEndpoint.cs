using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.RecordPerformedProcedure;

public sealed class RecordOwnPerformedProcedureEndpoint()
    : Endpoint<RecordOwnPerformedProcedureRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Post("/api/groomer/visits/{visitId:guid}/performed-procedures");
        Description(x => x.WithTags("Groomer Visits"));
        PermissionsAll("app.groomer.access", "groomer.visits.write");
    }

    public override async Task HandleAsync(RecordOwnPerformedProcedureRequest req, CancellationToken ct)
    {
        var result = await new RecordOwnPerformedProcedureUseCaseCommand(req.UserId, req.VisitId, req.VisitExecutionItemId, req.ProcedureId, req.Note).ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class RecordOwnPerformedProcedureRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid VisitId { get; set; }
    public Guid VisitExecutionItemId { get; set; }
    public Guid ProcedureId { get; set; }
    public string? Note { get; set; }
}

public sealed class RecordOwnPerformedProcedureRequestValidator : Validator<RecordOwnPerformedProcedureRequest>
{
    public RecordOwnPerformedProcedureRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.VisitExecutionItemId).NotEmpty();
        RuleFor(x => x.ProcedureId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
