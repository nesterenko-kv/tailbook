using Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

namespace Tailbook.Modules.Staff.Api.Admin;

internal static class GroomerResponseMapper
{
    public static CreateGroomerResponse ToCreateGroomerResponse(GroomerDetailView groomer)
        => new()
        {
            Id = groomer.Id,
            UserId = groomer.UserId,
            DisplayName = groomer.DisplayName,
            Active = groomer.Active,
            Capabilities = groomer.Capabilities.Select(ToGroomerCapabilityResponse).ToArray(),
            WorkingSchedules = groomer.WorkingSchedules.Select(ToWorkingScheduleResponse).ToArray(),
            CreatedAt = groomer.CreatedAt,
            UpdatedAt = groomer.UpdatedAt
        };

    public static GroomerCapabilityResponse ToGroomerCapabilityResponse(GroomerCapabilityView capability)
        => new()
        {
            Id = capability.Id,
            GroomerId = capability.GroomerId,
            AnimalTypeId = capability.AnimalTypeId,
            BreedId = capability.BreedId,
            BreedGroupId = capability.BreedGroupId,
            CoatTypeId = capability.CoatTypeId,
            SizeCategoryId = capability.SizeCategoryId,
            OfferId = capability.OfferId,
            CapabilityMode = capability.CapabilityMode,
            ReservedDurationModifierMinutes = capability.ReservedDurationModifierMinutes,
            Notes = capability.Notes,
            CreatedAt = capability.CreatedAt
        };

    public static WorkingScheduleResponse ToWorkingScheduleResponse(WorkingScheduleView schedule)
        => new()
        {
            Id = schedule.Id,
            GroomerId = schedule.GroomerId,
            Weekday = schedule.Weekday,
            StartLocalTime = schedule.StartLocalTime,
            EndLocalTime = schedule.EndLocalTime,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
}
