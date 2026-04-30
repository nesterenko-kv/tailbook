using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.ListOffers;

public sealed class ListOffersEndpoint(ICatalogQueries catalogQueries)
    : EndpointWithoutRequest<IReadOnlyCollection<OfferListItemResponse>>
{
    public override void Configure()
    {
        Get("/api/admin/catalog/offers");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var offers = await catalogQueries.ListOffersAsync(ct);
        await Send.OkAsync(offers.Select(x => new OfferListItemResponse
        {
            Id = x.Id,
            Code = x.Code,
            OfferType = x.OfferType,
            DisplayName = x.DisplayName,
            IsActive = x.IsActive,
            VersionCount = x.VersionCount,
            HasPublishedVersion = x.HasPublishedVersion,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        }).ToArray(), ct);
    }
}

public sealed class OfferListItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int VersionCount { get; set; }
    public bool HasPublishedVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
