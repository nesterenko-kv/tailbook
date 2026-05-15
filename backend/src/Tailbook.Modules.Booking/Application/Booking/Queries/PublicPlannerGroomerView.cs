namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PublicPlannerGroomerView(
    Guid GroomerId,
    string DisplayName,
    bool CanTakeRequest,
    int ReservedMinutes,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<PublicPlannerSlotView> Slots);