namespace Tailbook.Modules.Staff.Api.Admin.CheckAvailability;

public sealed class CheckAvailabilityRequest
{
    public Guid GroomerId { get; set; }
    public Guid PetId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public int ReservedMinutes { get; set; }
    public Guid[] OfferIds { get; set; } = [];
}