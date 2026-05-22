namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PublicBookingPlannerView(
    QuotePreviewView Quote,
    IReadOnlyCollection<PublicPlannerSlotView> AnySuitableSlots,
    IReadOnlyCollection<PublicPlannerGroomerView> Groomers);