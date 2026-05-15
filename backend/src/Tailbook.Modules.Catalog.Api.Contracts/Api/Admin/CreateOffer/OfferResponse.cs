namespace Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

public sealed class OfferResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public OfferVersionResponse[] Versions { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static OfferResponse Map(OfferDetailView view)
    {
        return new OfferResponse
        {
            Id = view.Id,
            Code = view.Code,
            OfferType = view.OfferType,
            DisplayName = view.DisplayName,
            IsActive = view.IsActive,
            Versions = view.Versions.Select(OfferVersionResponse.Map).ToArray(),
            CreatedAt = view.CreatedAt,
            UpdatedAt = view.UpdatedAt
        };
    }
}