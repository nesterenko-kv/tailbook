namespace Tailbook.Modules.Reporting.Domain;

public sealed class ReportingAppointment
{
    public Guid Id { get; set; }
    public DateTime StartAtUtc { get; set; }
}
