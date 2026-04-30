namespace Tailbook.Modules.Booking.Domain.Entities;

public sealed class DurationSnapshot
{
    public Guid Id { get; set; }
    public int ServiceMinutes { get; set; }
    public int ReservedMinutes { get; set; }
    public Guid? RuleSetId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
