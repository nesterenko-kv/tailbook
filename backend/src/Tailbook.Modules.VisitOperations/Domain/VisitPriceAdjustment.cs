namespace Tailbook.Modules.VisitOperations.Domain;

public sealed class VisitPriceAdjustment
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public int Sign { get; set; }
    public decimal Amount { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
