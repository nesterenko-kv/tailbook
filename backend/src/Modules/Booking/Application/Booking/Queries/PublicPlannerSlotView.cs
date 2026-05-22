namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PublicPlannerSlotView(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    IReadOnlyCollection<Guid> GroomerIds);