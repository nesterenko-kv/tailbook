namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicPlannerGroomerResponse
{
    public Guid GroomerId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool CanTakeRequest { get; set; }
    public int ReservedMinutes { get; set; }
    public string[] Reasons { get; set; } = [];
    public PublicPlannerSlotResponse[] Slots { get; set; } = [];
}