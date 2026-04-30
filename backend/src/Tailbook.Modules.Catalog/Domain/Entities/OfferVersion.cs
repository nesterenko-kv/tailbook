namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class OfferVersion
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public string? PolicyText { get; set; }
    public string? ChangeNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
