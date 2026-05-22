using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.ListOffers;

public sealed class ListOffersEndpoint(ICatalogReadService catalogReadService)
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
        var offers = await catalogReadService.ListOffersAsync(ct);
        await Send.OkAsync(offers.Select(x => new OfferListItemResponse
        {
            Id = x.Id,
            Code = x.Code,
            OfferType = x.OfferType,
            DisplayName = x.DisplayName,
            IsActive = x.IsActive,
            VersionCount = x.VersionCount,
            HasPublishedVersion = x.HasPublishedVersion,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToArray(), ct);
    }
}