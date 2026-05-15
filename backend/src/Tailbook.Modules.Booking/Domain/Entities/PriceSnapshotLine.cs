namespace Tailbook.Modules.Booking.Domain.Entities;

public sealed class PriceSnapshotLine
{
    public Guid Id { get; set; }
    public Guid PriceSnapshotId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? SourceRuleId { get; set; }
    public int SequenceNo { get; set; }
}
