namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record VisitExpectedComponentView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string ComponentRole, int SequenceNo, bool DefaultExpected, bool IsSkipped);