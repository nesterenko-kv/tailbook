namespace Tailbook.Modules.Catalog.Domain.Aggregates;

public sealed class CommercialOffer
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
