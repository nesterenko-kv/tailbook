namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record DurationSnapshotView(Guid Id, int ServiceMinutes, int ReservedMinutes, IReadOnlyCollection<DurationSnapshotLineView> Lines);