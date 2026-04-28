using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class PreviewMyQuoteEndpoint(
    IClientPortalActorService actorService,
    ClientPortalBookingQueries queries)
    : Endpoint<PreviewMyQuoteRequest, PreviewMyQuoteResponse>
{
    public override void Configure()
    {
        Post("/api/client/quotes/preview");
        Description(x => x.WithTags("Client Portal Booking"));
        Permissions("client.booking.write");
    }

    public override async Task HandleAsync(PreviewMyQuoteRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            var result = await queries.PreviewMyQuoteAsync(
                actor,
                new PreviewQuoteCommand(req.PetId, null,
                    req.Items.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray()),
                ct);

            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(new PreviewMyQuoteResponse
            {
                Currency = result.PriceSnapshot.Currency,
                TotalAmount = result.PriceSnapshot.TotalAmount,
                ServiceMinutes = result.DurationSnapshot.ServiceMinutes,
                ReservedMinutes = result.DurationSnapshot.ReservedMinutes,
                Items = result.Items.Select(x => new PreviewMyQuoteResponse.QuoteItemPayload
                {
                    OfferId = x.OfferId,
                    OfferType = x.OfferType,
                    DisplayName = x.DisplayName,
                    PriceAmount = x.PriceAmount,
                    ServiceMinutes = x.ServiceMinutes,
                    ReservedMinutes = x.ReservedMinutes
                }).ToArray(),
                PriceLines = result.PriceSnapshot.Lines.Select(x => new PreviewMyQuoteResponse.PriceLinePayload
                {
                    Label = x.Label,
                    Amount = x.Amount
                }).ToArray(),
                DurationLines = result.DurationSnapshot.Lines.Select(x => new PreviewMyQuoteResponse.DurationLinePayload
                {
                    Label = x.Label,
                    Minutes = x.Minutes
                }).ToArray()
            }, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}
