namespace Tailbook.Modules.Reporting.Domain;

public sealed class ReportingVisitPriceAdjustment
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public int Sign { get; set; }
    public decimal Amount { get; set; }
}
