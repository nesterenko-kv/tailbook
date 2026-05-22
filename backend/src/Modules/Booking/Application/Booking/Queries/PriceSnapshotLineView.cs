namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PriceSnapshotLineView(string LineType, string Label, decimal Amount, Guid? SourceRuleId, int SequenceNo);