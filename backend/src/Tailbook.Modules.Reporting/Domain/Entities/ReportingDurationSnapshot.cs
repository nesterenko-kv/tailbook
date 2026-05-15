namespace Tailbook.Modules.Reporting.Domain.Entities;

public sealed class ReportingDurationSnapshot
{
    public Guid Id { get; set; }
    public int ServiceMinutes { get; set; }
    public int ReservedMinutes { get; set; }
}
