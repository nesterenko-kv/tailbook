namespace Tailbook.Modules.VisitOperations.Domain.ValueObjects;

public sealed record VisitPerformedProcedureDraft(
    Guid ProcedureId,
    string ProcedureCodeSnapshot,
    string ProcedureNameSnapshot,
    string? Note);
