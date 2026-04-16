namespace Tailbook.Modules.VisitOperations.Domain;

public sealed class VisitPerformedProcedure
{
    public Guid Id { get; set; }
    public Guid VisitExecutionItemId { get; set; }
    public Guid ProcedureId { get; set; }
    public string ProcedureCodeSnapshot { get; set; } = string.Empty;
    public string ProcedureNameSnapshot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
