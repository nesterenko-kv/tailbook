namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record DurationSnapshotLineView(string LineType, string Label, int Minutes, Guid? SourceRuleId, int SequenceNo);