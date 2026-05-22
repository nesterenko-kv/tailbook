namespace Tailbook.Modules.Staff.Application.Staff.Models;

public sealed record GroomerDetailView(Guid Id, Guid? UserId, string DisplayName, bool Active, IReadOnlyCollection<GroomerCapabilityView> Capabilities, IReadOnlyCollection<WorkingScheduleView> WorkingSchedules, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);