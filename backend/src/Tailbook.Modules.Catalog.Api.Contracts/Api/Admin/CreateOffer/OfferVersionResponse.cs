namespace Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

public sealed class OfferVersionResponse
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public string? PolicyText { get; set; }
    public string? ChangeNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public OfferVersionComponentResponse[] Components { get; set; } = [];

    public static OfferVersionResponse Map(OfferVersionView view)
    {
        return new OfferVersionResponse
        {
            Id = view.Id,
            OfferId = view.OfferId,
            VersionNo = view.VersionNo,
            Status = view.Status,
            ValidFrom = view.ValidFrom,
            ValidTo = view.ValidTo,
            PolicyText = view.PolicyText,
            ChangeNote = view.ChangeNote,
            CreatedAt = view.CreatedAt,
            PublishedAt = view.PublishedAt,
            Components = view.Components.Select(OfferVersionComponentResponse.Map).ToArray()
        };
    }
}