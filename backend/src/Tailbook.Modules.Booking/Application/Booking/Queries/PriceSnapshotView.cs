namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PriceSnapshotView(Guid Id, string Currency, decimal TotalAmount, IReadOnlyCollection<PriceSnapshotLineView> Lines);