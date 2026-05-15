namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record GroomerVisitSkippedComponentView(Guid Id, Guid OfferVersionComponentId, Guid ProcedureId, string ProcedureCode, string ProcedureName, string OmissionReasonCode, string? Note, DateTimeOffset RecordedAt);