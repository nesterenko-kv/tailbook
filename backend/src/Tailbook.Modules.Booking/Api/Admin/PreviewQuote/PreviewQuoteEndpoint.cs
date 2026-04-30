using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.PreviewQuote;

public sealed class PreviewQuoteEndpoint(BookingQuoteQueries bookingQuoteQueries)
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
        var result = await bookingQuoteQueries.PreviewQuoteAsync(
            new PreviewQuoteCommand(req.PetId, req.GroomerId, req.Items.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray()),
            req.ActorUserId?.ToString("D"),
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

public sealed class PreviewQuoteRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid PetId { get; set; }
    public Guid? GroomerId { get; set; }
    public PreviewQuoteItemRequest[] Items { get; set; } = [];
}

public sealed class PreviewQuoteItemRequest
{
    public string? ItemType { get; set; }
    public Guid OfferId { get; set; }
}

public sealed class PreviewQuoteRequestValidator : Validator<PreviewQuoteRequest>
{
    public PreviewQuoteRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.OfferId).NotEmpty();
            item.RuleFor(x => x.ItemType).MaximumLength(32);
        });
    }
}

public sealed class PreviewQuoteResponse
{
    public PriceSnapshotPayload PriceSnapshot { get; set; } = new();
    public DurationSnapshotPayload DurationSnapshot { get; set; } = new();
    public QuotePreviewItemPayload[] Items { get; set; } = [];

    public sealed class PriceSnapshotPayload
    {
        public Guid Id { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public PriceSnapshotLinePayload[] Lines { get; set; } = [];
    }

    public sealed class PriceSnapshotLinePayload
    {
        public string LineType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid? SourceRuleId { get; set; }
        public int SequenceNo { get; set; }
    }

    public sealed class DurationSnapshotPayload
    {
        public Guid Id { get; set; }
        public int ServiceMinutes { get; set; }
        public int ReservedMinutes { get; set; }
        public DurationSnapshotLinePayload[] Lines { get; set; } = [];
    }

    public sealed class DurationSnapshotLinePayload
    {
        public string LineType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Minutes { get; set; }
        public Guid? SourceRuleId { get; set; }
        public int SequenceNo { get; set; }
    }

    public sealed class QuotePreviewItemPayload
    {
        public Guid OfferId { get; set; }
        public Guid OfferVersionId { get; set; }
        public string OfferCode { get; set; } = string.Empty;
        public string OfferType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public int ServiceMinutes { get; set; }
        public int ReservedMinutes { get; set; }
    }
}
