namespace Tailbook.Modules.Reporting.Domain;

public sealed class ReportingPriceSnapshot
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
}
