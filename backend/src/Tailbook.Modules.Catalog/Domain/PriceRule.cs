namespace Tailbook.Modules.Catalog.Domain;

public sealed class PriceRule
{
    public Guid Id { get; set; }
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public int Priority { get; set; }
    public int SpecificityScore { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public decimal FixedAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
