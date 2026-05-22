namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record GroomerVisitPerformedProcedureView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string Status, string? Note, DateTimeOffset RecordedAt);