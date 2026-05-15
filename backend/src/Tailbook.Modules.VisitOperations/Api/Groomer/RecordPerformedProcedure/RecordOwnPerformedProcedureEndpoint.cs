using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.RecordPerformedProcedure;

public sealed class RecordOwnPerformedProcedureEndpoint : Endpoint<RecordOwnPerformedProcedureRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Post("/api/groomer/visits/{visitId:guid}/performed-procedures");
        Description(x => x.WithTags("Groomer Visits"));
        PermissionsAll("app.groomer.access", "groomer.visits.write");
    }

    public override async Task HandleAsync(RecordOwnPerformedProcedureRequest req, CancellationToken ct)
    {
        var command = new RecordOwnPerformedProcedureUseCaseCommand(req.UserId, req.VisitId, req.VisitExecutionItemId, req.ProcedureId, req.Note);

        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}
