namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PreviewQuoteQuery(Guid PetId, Guid? GroomerId, IReadOnlyCollection<PreviewQuoteItemQuery> Items);