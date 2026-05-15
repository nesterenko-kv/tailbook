namespace Tailbook.Modules.Reporting.Domain.Entities;

public sealed class ReportingPriceSnapshot
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
}
