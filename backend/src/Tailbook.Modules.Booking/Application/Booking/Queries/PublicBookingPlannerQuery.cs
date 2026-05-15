namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PublicBookingPlannerQuery(
    PublicPetSelectionQuery Pet,
    DateOnly LocalDate,
    IReadOnlyCollection<PreviewQuoteItemQuery> Items);