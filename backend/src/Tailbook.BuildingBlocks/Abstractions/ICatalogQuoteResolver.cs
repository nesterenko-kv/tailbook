using ErrorOr;

namespace Tailbook.BuildingBlocks.Abstractions;

public interface ICatalogQuoteResolver
{
    Task<ErrorOr<CatalogQuoteResolution>> ResolveAsync(
        PetQuoteProfile pet,
        IReadOnlyCollection<QuotePreviewCatalogItem> items,
        CancellationToken cancellationToken);
}

public sealed record QuotePreviewCatalogItem(Guid OfferId, string? ItemType);

public sealed record CatalogQuoteResolution(
    Guid? PriceRuleSetId,
    Guid? DurationRuleSetId,
    string Currency,
    decimal TotalAmount,
    int ServiceMinutes,
    int ReservedMinutes,
    IReadOnlyCollection<CatalogQuotePriceLine> PriceLines,
    IReadOnlyCollection<CatalogQuoteDurationLine> DurationLines,
    IReadOnlyCollection<CatalogResolvedQuoteItem> Items);

public sealed record CatalogResolvedQuoteItem(
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCode,
    string OfferType,
    string DisplayName,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);

public sealed record CatalogQuotePriceLine(
    Guid OfferId,
    Guid OfferVersionId,
    string LineType,
    string Label,
    decimal Amount,
    Guid? SourceRuleId,
    int SequenceNo);

public sealed record CatalogQuoteDurationLine(
    Guid OfferId,
    Guid OfferVersionId,
    string LineType,
    string Label,
    int Minutes,
    Guid? SourceRuleId,
    int SequenceNo);
