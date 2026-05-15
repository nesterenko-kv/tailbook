namespace Tailbook.Modules.Catalog.Api.Admin.CreateOfferVersion;

public sealed class CreateOfferVersionRequest
{
    public Guid OfferId { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public string? PolicyText { get; set; }
    public string? ChangeNote { get; set; }
}