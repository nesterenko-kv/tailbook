using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PreviewPublicQuoteEndpoint(
    IClientPortalActorService actorService,
    PublicBookingReadService queries)
    : Endpoint<PublicPreviewQuoteRequest, PublicQuotePreviewResponse>
{
    public override void Configure()
    {
        Post("/api/public/quotes/preview");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(PublicPreviewQuoteRequest req, CancellationToken ct)
    {
        var actor = await PublicBookingEndpointMapper.ResolveActorAsync(req.UserId, actorService, ct);
        var result = await queries.PreviewQuoteAsync(
            actor,
            new PublicPreviewQuoteQuery(
                PublicBookingEndpointMapper.MapPet(req.Pet),
                req.Items.Select(x => new PreviewQuoteItemQuery(x.OfferId, x.ItemType)).ToArray()),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(PublicBookingEndpointMapper.MapQuote(result.Value), ct);
    }
}