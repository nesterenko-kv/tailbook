namespace Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

public sealed class CreateOfferRequest
{
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}