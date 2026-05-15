using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.PreviewQuote;

public sealed class PreviewQuoteEndpoint(BookingQuoteReadService bookingQuoteReadService)
    : Endpoint<PreviewQuoteRequest, PreviewQuoteResponse>
{
    public override void Configure()
    {
        Post("/api/admin/quotes/preview");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.read", "catalog.read", "pets.read");
    }

    public override async Task HandleAsync(PreviewQuoteRequest req, CancellationToken ct)
    {
        var result = await bookingQuoteReadService.PreviewQuoteAsync(
            new PreviewQuoteQuery(req.PetId, req.GroomerId, req.Items.Select(x => new PreviewQuoteItemQuery(x.OfferId, x.ItemType)).ToArray()),
            req.ActorUserId,
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(new PreviewQuoteResponse
        {
            PriceSnapshot = new PreviewQuoteResponse.PriceSnapshotPayload
            {
                Id = result.Value.PriceSnapshot.Id,
                Currency = result.Value.PriceSnapshot.Currency,
                TotalAmount = result.Value.PriceSnapshot.TotalAmount,
                Lines = result.Value.PriceSnapshot.Lines.Select(x => new PreviewQuoteResponse.PriceSnapshotLinePayload
                {
                    LineType = x.LineType,
                    Label = x.Label,
                    Amount = x.Amount,
                    SourceRuleId = x.SourceRuleId,
                    SequenceNo = x.SequenceNo
                }).ToArray()
            },
            DurationSnapshot = new PreviewQuoteResponse.DurationSnapshotPayload
            {
                Id = result.Value.DurationSnapshot.Id,
                ServiceMinutes = result.Value.DurationSnapshot.ServiceMinutes,
                ReservedMinutes = result.Value.DurationSnapshot.ReservedMinutes,
                Lines = result.Value.DurationSnapshot.Lines.Select(x => new PreviewQuoteResponse.DurationSnapshotLinePayload
                {
                    LineType = x.LineType,
                    Label = x.Label,
                    Minutes = x.Minutes,
                    SourceRuleId = x.SourceRuleId,
                    SequenceNo = x.SequenceNo
                }).ToArray()
            },
            Items = result.Value.Items.Select(x => new PreviewQuoteResponse.QuotePreviewItemPayload
            {
                OfferId = x.OfferId,
                OfferVersionId = x.OfferVersionId,
                OfferCode = x.OfferCode,
                OfferType = x.OfferType,
                DisplayName = x.DisplayName,
                PriceAmount = x.PriceAmount,
                ServiceMinutes = x.ServiceMinutes,
                ReservedMinutes = x.ReservedMinutes
            }).ToArray()
        }, cancellation: ct);
    }
}
