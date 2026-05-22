namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record GroomerVisitExpectedComponentView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string ComponentRole, int SequenceNo, bool DefaultExpected, bool IsSkipped);