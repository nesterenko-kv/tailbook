namespace Tailbook.Modules.Reporting.Domain;

public sealed class ReportingVisitExecutionItem
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public Guid AppointmentItemId { get; set; }
}
