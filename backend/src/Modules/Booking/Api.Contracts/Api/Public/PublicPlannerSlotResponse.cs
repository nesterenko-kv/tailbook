namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicPlannerSlotResponse
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public Guid[] GroomerIds { get; set; } = [];
}