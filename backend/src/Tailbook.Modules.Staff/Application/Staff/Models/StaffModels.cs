namespace Tailbook.Modules.Staff.Application.Staff.Models;

public sealed record GroomerListItemView(Guid Id, Guid? UserId, string DisplayName, bool Active, int CapabilityCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record GroomerDetailView(Guid Id, Guid? UserId, string DisplayName, bool Active, IReadOnlyCollection<GroomerCapabilityView> Capabilities, IReadOnlyCollection<WorkingScheduleView> WorkingSchedules, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record GroomerCapabilityView(Guid Id, Guid GroomerId, Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId, Guid? OfferId, string CapabilityMode, int ReservedDurationModifierMinutes, string? Notes, DateTime CreatedAtUtc);
public sealed record WorkingScheduleView(Guid Id, Guid GroomerId, int Weekday, string StartLocalTime, string EndLocalTime, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record TimeBlockView(Guid Id, Guid GroomerId, DateTime StartAtUtc, DateTime EndAtUtc, string ReasonCode, string? Notes, DateTime CreatedAtUtc);
public sealed record AvailabilityWindowView(DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc);
public sealed record GroomerScheduleView(Guid GroomerId, string GroomerDisplayName, DateTimeOffset FromUtc, DateTimeOffset ToUtc, IReadOnlyCollection<WorkingScheduleView> WorkingSchedules, IReadOnlyCollection<TimeBlockView> TimeBlocks, IReadOnlyCollection<AvailabilityWindowView> AvailabilityWindows);
