using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.RecordPerformedProcedure;

public sealed class RecordOwnPerformedProcedureRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid VisitId { get; set; }
    public Guid VisitExecutionItemId { get; set; }
    public Guid ProcedureId { get; set; }
    public string? Note { get; set; }
}