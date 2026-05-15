using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.RecordPerformedProcedure;

public sealed class RecordPerformedProcedureEndpoint : Endpoint<RecordPerformedProcedureRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/visits/{visitId:guid}/performed-procedures");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.write");
    }

    public override async Task HandleAsync(RecordPerformedProcedureRequest req, CancellationToken ct)
    {
        var command = new RecordPerformedProcedureUseCaseCommand(req.VisitId, req.VisitExecutionItemId, req.ProcedureId, req.Note, req.ActorUserId);

        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}
