using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class PreviewMyQuoteEndpoint(
    IClientPortalActorService actorService,
    IClientPortalBookingQueries queries)
    : Endpoint<PreviewMyQuoteRequest, PreviewMyQuoteResponse>
{
    public override void Configure()
    {
        Post("/api/client/quotes/preview");
        Description(x => x.WithTags("Client Portal Booking"));
        PermissionsAll("client.booking.write");
    }

    public override async Task HandleAsync(PreviewMyQuoteRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.PreviewMyQuoteAsync(
            actor,
            new PreviewQuoteCommand(req.PetId, null,
                req.Items.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray()),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(new PreviewMyQuoteResponse
        {
            Currency = result.Value.PriceSnapshot.Currency,
            TotalAmount = result.Value.PriceSnapshot.TotalAmount,
            ServiceMinutes = result.Value.DurationSnapshot.ServiceMinutes,
            ReservedMinutes = result.Value.DurationSnapshot.ReservedMinutes,
            Items = result.Value.Items.Select(x => new PreviewMyQuoteResponse.QuoteItemPayload
            {
                OfferId = x.OfferId,
                OfferType = x.OfferType,
                DisplayName = x.DisplayName,
                PriceAmount = x.PriceAmount,
                ServiceMinutes = x.ServiceMinutes,
                ReservedMinutes = x.ReservedMinutes
            }).ToArray(),
            PriceLines = result.Value.PriceSnapshot.Lines.Select(x => new PreviewMyQuoteResponse.PriceLinePayload
            {
                Label = x.Label,
                Amount = x.Amount
            }).ToArray(),
            DurationLines = result.Value.DurationSnapshot.Lines.Select(x => new PreviewMyQuoteResponse.DurationLinePayload
            {
                Label = x.Label,
                Minutes = x.Minutes
            }).ToArray()
        }, ct);
    }
}
