namespace Tailbook.Modules.Catalog.Domain;

public sealed class PriceRuleSet
{
    public Guid Id { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
