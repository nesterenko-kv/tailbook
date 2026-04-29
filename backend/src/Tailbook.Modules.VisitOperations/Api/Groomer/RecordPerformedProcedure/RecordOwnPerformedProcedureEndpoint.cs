using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.RecordPerformedProcedure;

public sealed class RecordOwnPerformedProcedureEndpoint(GroomerVisitQueries groomerVisitQueries)
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
        try
        {
            var result = await groomerVisitQueries.RecordPerformedProcedureAsync(req.UserId, req.VisitId, req.VisitExecutionItemId, req.ProcedureId, req.Note, ct);
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
