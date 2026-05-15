namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicBookingPlannerResponse
{
    public PublicQuotePreviewResponse Quote { get; set; } = new();
    public PublicPlannerSlotResponse[] AnySuitableSlots { get; set; } = [];
    public PublicPlannerGroomerResponse[] Groomers { get; set; } = [];
}