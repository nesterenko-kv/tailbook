namespace Tailbook.Modules.VisitOperations.Domain;

public sealed record VisitSkippedComponentDraft(
    Guid OfferVersionComponentId,
    Guid ProcedureId,
    string ProcedureCodeSnapshot,
    string ProcedureNameSnapshot,
    string? OmissionReasonCode,
    string? Note);
