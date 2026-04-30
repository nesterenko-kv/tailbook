namespace Tailbook.Modules.Booking.Domain.Entities;

public sealed class PriceSnapshot
{
    public Guid Id { get; set; }
    public string SnapshotType { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public Guid? RuleSetId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
