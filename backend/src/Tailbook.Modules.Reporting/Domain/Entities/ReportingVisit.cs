namespace Tailbook.Modules.Reporting.Domain.Entities;

public sealed class ReportingVisit
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}
