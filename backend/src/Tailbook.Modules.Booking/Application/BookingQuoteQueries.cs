using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

public sealed class BookingQuoteQueries(AppDbContext dbContext, IPetQuoteProfileService petQuoteProfileService, ICatalogQuoteResolver catalogQuoteResolver)
{
    public async Task<QuotePreviewView> PreviewQuoteAsync(PreviewQuoteCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(command.PetId, cancellationToken)
            ?? throw new InvalidOperationException("Pet does not exist.");

        var resolution = await catalogQuoteResolver.ResolveAsync(
            pet,
            command.Items.Select(x => new QuotePreviewCatalogItem(x.OfferId, x.ItemType)).ToArray(),
            cancellationToken);

        var actorGuid = ParseGuid(actorUserId);
        var utcNow = DateTime.UtcNow;

        var priceSnapshot = new PriceSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = SnapshotTypeCodes.QuotePreview,
            Currency = resolution.Currency,
            TotalAmount = resolution.TotalAmount,
            RuleSetId = resolution.PriceRuleSetId,
            CreatedByUserId = actorGuid,
            CreatedAtUtc = utcNow
        };

        var durationSnapshot = new DurationSnapshot
        {
            Id = Guid.NewGuid(),
            ServiceMinutes = resolution.ServiceMinutes,
            ReservedMinutes = resolution.ReservedMinutes,
            RuleSetId = resolution.DurationRuleSetId,
            CreatedByUserId = actorGuid,
            CreatedAtUtc = utcNow
        };

        dbContext.Set<PriceSnapshot>().Add(priceSnapshot);
        dbContext.Set<DurationSnapshot>().Add(durationSnapshot);

        dbContext.Set<PriceSnapshotLine>().AddRange(resolution.PriceLines.Select(x => new PriceSnapshotLine
        {
            Id = Guid.NewGuid(),
            PriceSnapshotId = priceSnapshot.Id,
            LineType = x.LineType,
            Label = x.Label,
            Amount = x.Amount,
            SourceRuleId = x.SourceRuleId,
            SequenceNo = x.SequenceNo
        }));

        dbContext.Set<DurationSnapshotLine>().AddRange(resolution.DurationLines.Select(x => new DurationSnapshotLine
        {
            Id = Guid.NewGuid(),
            DurationSnapshotId = durationSnapshot.Id,
            LineType = x.LineType,
            Label = x.Label,
            Minutes = x.Minutes,
            SourceRuleId = x.SourceRuleId,
            SequenceNo = x.SequenceNo
        }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new QuotePreviewView(
            new PriceSnapshotView(
                priceSnapshot.Id,
                priceSnapshot.Currency,
                priceSnapshot.TotalAmount,
                resolution.PriceLines.Select(x => new PriceSnapshotLineView(x.LineType, x.Label, x.Amount, x.SourceRuleId, x.SequenceNo)).ToArray()),
            new DurationSnapshotView(
                durationSnapshot.Id,
                durationSnapshot.ServiceMinutes,
                durationSnapshot.ReservedMinutes,
                resolution.DurationLines.Select(x => new DurationSnapshotLineView(x.LineType, x.Label, x.Minutes, x.SourceRuleId, x.SequenceNo)).ToArray()),
            resolution.Items.Select(x => new QuotePreviewItemView(x.OfferId, x.OfferVersionId, x.OfferCode, x.OfferType, x.DisplayName, x.PriceAmount, x.ServiceMinutes, x.ReservedMinutes)).ToArray());
    }

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public sealed record PreviewQuoteCommand(Guid PetId, IReadOnlyCollection<PreviewQuoteItemCommand> Items);
public sealed record PreviewQuoteItemCommand(Guid OfferId, string? ItemType);
public sealed record QuotePreviewView(PriceSnapshotView PriceSnapshot, DurationSnapshotView DurationSnapshot, IReadOnlyCollection<QuotePreviewItemView> Items);
public sealed record QuotePreviewItemView(Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferType, string DisplayName, decimal PriceAmount, int ServiceMinutes, int ReservedMinutes);
public sealed record PriceSnapshotView(Guid Id, string Currency, decimal TotalAmount, IReadOnlyCollection<PriceSnapshotLineView> Lines);
public sealed record PriceSnapshotLineView(string LineType, string Label, decimal Amount, Guid? SourceRuleId, int SequenceNo);
public sealed record DurationSnapshotView(Guid Id, int ServiceMinutes, int ReservedMinutes, IReadOnlyCollection<DurationSnapshotLineView> Lines);
public sealed record DurationSnapshotLineView(string LineType, string Label, int Minutes, Guid? SourceRuleId, int SequenceNo);
