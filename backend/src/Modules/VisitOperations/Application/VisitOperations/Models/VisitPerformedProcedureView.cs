namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record VisitPerformedProcedureView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string Status, string? Note, DateTimeOffset RecordedAt);