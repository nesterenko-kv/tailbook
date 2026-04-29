namespace Tailbook.Modules.VisitOperations.Domain;

public sealed record VisitPerformedProcedureDraft(
    Guid ProcedureId,
    string ProcedureCodeSnapshot,
    string ProcedureNameSnapshot,
    string? Note);
