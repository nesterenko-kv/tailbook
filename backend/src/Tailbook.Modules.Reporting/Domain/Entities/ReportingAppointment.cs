namespace Tailbook.Modules.Reporting.Domain.Entities;

public sealed class ReportingAppointment
{
    public Guid Id { get; set; }
    public DateTime StartAtUtc { get; set; }
}
