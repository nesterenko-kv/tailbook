namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PublicPreviewQuoteQuery(
    PublicPetSelectionQuery Pet,
    IReadOnlyCollection<PreviewQuoteItemQuery> Items);