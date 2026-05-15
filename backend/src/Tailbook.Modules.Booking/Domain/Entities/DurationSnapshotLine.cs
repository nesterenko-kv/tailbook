namespace Tailbook.Modules.Booking.Domain.Entities;

public sealed class DurationSnapshotLine
{
    public Guid Id { get; set; }
    public Guid DurationSnapshotId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public Guid? SourceRuleId { get; set; }
    public int SequenceNo { get; set; }
}
