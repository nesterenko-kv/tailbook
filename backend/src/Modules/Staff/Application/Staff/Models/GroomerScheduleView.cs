namespace Tailbook.Modules.Staff.Application.Staff.Models;

public sealed record GroomerScheduleView(Guid GroomerId, string GroomerDisplayName, DateTimeOffset From, DateTimeOffset To, IReadOnlyCollection<WorkingScheduleView> WorkingSchedules, IReadOnlyCollection<TimeBlockView> TimeBlocks, IReadOnlyCollection<AvailabilityWindowView> AvailabilityWindows);