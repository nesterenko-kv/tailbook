namespace Tailbook.Modules.Reporting.Domain.Entities;

public sealed class ReportingVisitSkippedComponent
{
    public Guid Id { get; set; }
    public Guid VisitExecutionItemId { get; set; }
}
