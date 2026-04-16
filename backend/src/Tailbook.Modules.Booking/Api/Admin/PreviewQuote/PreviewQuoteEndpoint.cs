using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.PreviewQuote;

public sealed class PreviewQuoteEndpoint(ICurrentUser currentUser, IBookingAccessPolicy accessPolicy, BookingQuoteQueries bookingQuoteQueries)
    : Endpoint<PreviewQuoteRequest, PreviewQuoteResponse>
{
    public override void Configure()
    {
        Post("/api/admin/quotes/preview");
        Description(x => x.WithTags("Admin Booking"));
    }

    public override async Task HandleAsync(PreviewQuoteRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanPreviewQuotes(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await bookingQuoteQueries.PreviewQuoteAsync(
                new PreviewQuoteCommand(req.PetId, req.Items.Select(x => new PreviewQuoteItemCommand(x.OfferId, x.ItemType)).ToArray()),
                currentUser.UserId,
                ct);

            await Send.ResponseAsync(new PreviewQuoteResponse
            {
                PriceSnapshot = new PreviewQuoteResponse.PriceSnapshotPayload
                {
                    Id = result.PriceSnapshot.Id,
                    Currency = result.PriceSnapshot.Currency,
                    TotalAmount = result.PriceSnapshot.TotalAmount,
                    Lines = result.PriceSnapshot.Lines.Select(x => new PreviewQuoteResponse.PriceSnapshotLinePayload
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
                    Id = result.DurationSnapshot.Id,
                    ServiceMinutes = result.DurationSnapshot.ServiceMinutes,
                    ReservedMinutes = result.DurationSnapshot.ReservedMinutes,
                    Lines = result.DurationSnapshot.Lines.Select(x => new PreviewQuoteResponse.DurationSnapshotLinePayload
                    {
                        LineType = x.LineType,
                        Label = x.Label,
                        Minutes = x.Minutes,
                        SourceRuleId = x.SourceRuleId,
                        SequenceNo = x.SequenceNo
                    }).ToArray()
                },
                Items = result.Items.Select(x => new PreviewQuoteResponse.QuotePreviewItemPayload
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
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class PreviewQuoteRequest
{
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
        RuleForEach(x => x.Items).SetValidator(new PreviewQuoteItemRequestValidator());
    }
}

public sealed class PreviewQuoteItemRequestValidator : AbstractValidator<PreviewQuoteItemRequest>
{
    public PreviewQuoteItemRequestValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ItemType).MaximumLength(32);
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
