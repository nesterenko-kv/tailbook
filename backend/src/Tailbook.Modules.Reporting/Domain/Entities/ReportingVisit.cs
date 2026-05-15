namespace Tailbook.Modules.Reporting.Domain.Entities;

public sealed class ReportingVisit
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
