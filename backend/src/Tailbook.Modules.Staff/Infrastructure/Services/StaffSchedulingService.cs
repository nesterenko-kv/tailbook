using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Staff.Contracts;

namespace Tailbook.Modules.Staff.Infrastructure.Services;

public sealed class StaffSchedulingService(
    AppDbContext dbContext,
    IPetQuoteProfileService petQuoteProfileService,
    SalonTimeZoneProvider salonTimeZoneProvider,
    IAppointmentOverlapReadService appointmentOverlapReadService)
    : IStaffSchedulingService
{
    public async Task<ErrorOr<ReservedDurationResolution>> ResolveReservedDurationAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        int baseReservedMinutes,
        CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(petId, cancellationToken);
        if (pet is null)
        {
            return Error.NotFound("Staff.PetNotFound", "Pet does not exist.");
        }

        return await ResolveReservedDurationAsync(groomerId, pet, offerIds, baseReservedMinutes, cancellationToken);
    }

    public async Task<ErrorOr<ReservedDurationResolution>> ResolveReservedDurationAsync(
        Guid groomerId,
        PetQuoteProfile pet,
        IReadOnlyCollection<Guid> offerIds,
        int baseReservedMinutes,
        CancellationToken cancellationToken)
    {
        var groomer = await dbContext.Set<Groomer>()
            .SingleOrDefaultAsync(x => x.Id == groomerId && x.Active, cancellationToken);
        if (groomer is null)
        {
            return Error.NotFound("Staff.GroomerNotFound", "Selected groomer does not exist or is inactive.");
        }

        var capabilities = await dbContext.Set<GroomerCapability>()
            .Where(x => x.GroomerId == groomer.Id)
            .ToListAsync(cancellationToken);

        var denial = capabilities
            .Where(x => IsMatch(x, pet, offerIds))
            .Where(x => string.Equals(x.CapabilityMode, CapabilityModeCodes.Deny, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(ComputeSpecificity)
            .FirstOrDefault();

        if (denial is not null)
        {
            return Error.Validation("Staff.GroomerCapabilityDenied", BuildCapabilityReason(denial, pet, false));
        }

        var modifierReasons = new List<string>();
        var modifierMinutes = 0;
        var distinctOfferIds = offerIds.Distinct().ToArray();

        if (distinctOfferIds.Length == 0)
        {
            var generalRule = capabilities
                .Where(x => x.OfferId is null)
                .Where(x => string.Equals(x.CapabilityMode, CapabilityModeCodes.Allow, StringComparison.OrdinalIgnoreCase))
                .Where(x => IsMatch(x, pet, []))
                .OrderByDescending(ComputeSpecificity)
                .FirstOrDefault();

            if (generalRule is not null && generalRule.ReservedDurationModifierMinutes != 0)
            {
                modifierMinutes += generalRule.ReservedDurationModifierMinutes;
                modifierReasons.Add(BuildModifierReason(generalRule, pet));
            }
        }
        else
        {
            foreach (var offerId in distinctOfferIds)
            {
                var matched = capabilities
                    .Where(x => x.OfferId is null || x.OfferId == offerId)
                    .Where(x => string.Equals(x.CapabilityMode, CapabilityModeCodes.Allow, StringComparison.OrdinalIgnoreCase))
                    .Where(x => IsMatch(x, pet, [offerId]))
                    .OrderByDescending(x => x.OfferId == offerId ? 1 : 0)
                    .ThenByDescending(ComputeSpecificity)
                    .FirstOrDefault();

                if (matched is not null && matched.ReservedDurationModifierMinutes != 0)
                {
                    modifierMinutes += matched.ReservedDurationModifierMinutes;
                    modifierReasons.Add(BuildModifierReason(matched, pet));
                }
            }
        }

        var effectiveReservedMinutes = Math.Max(15, baseReservedMinutes + modifierMinutes);
        return new ReservedDurationResolution(baseReservedMinutes, effectiveReservedMinutes, modifierMinutes, modifierReasons);
    }

    public async Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        DateTime startAtUtc,
        int reservedMinutes,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(petId, cancellationToken);
        if (pet is null)
        {
            return Error.NotFound("Staff.PetNotFound", "Pet does not exist.");
        }

        return await CheckAvailabilityAsync(groomerId, pet, offerIds, startAtUtc, reservedMinutes, ignoredAppointmentId, cancellationToken);
    }

    public async Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(
        Guid groomerId,
        PetQuoteProfile pet,
        IReadOnlyCollection<Guid> offerIds,
        DateTime startAtUtc,
        int reservedMinutes,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        var normalizedStartAtUtc = DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc);
        var durationResolutionResult = await ResolveReservedDurationAsync(groomerId, pet, offerIds, reservedMinutes, cancellationToken);
        if (durationResolutionResult.IsError)
        {
            return durationResolutionResult.Errors;
        }

        var durationResolution = durationResolutionResult.Value;
        var endAtUtc = normalizedStartAtUtc.AddMinutes(durationResolution.EffectiveReservedMinutes);

        var reasons = new List<string>(durationResolution.Reasons);

        var groomer = await dbContext.Set<Groomer>().SingleOrDefaultAsync(x => x.Id == groomerId && x.Active, cancellationToken);
        if (groomer is null)
        {
            reasons.Add("Selected groomer does not exist or is inactive.");
            return new GroomerAvailabilityCheckResult(false, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
        }

        var schedule = await dbContext.Set<WorkingSchedule>()
            .Where(x => x.GroomerId == groomerId)
            .ToListAsync(cancellationToken);

        if (!IsInsideWorkingSchedule(normalizedStartAtUtc, endAtUtc, schedule))
        {
            reasons.Add("Requested slot is outside working schedule.");
            return new GroomerAvailabilityCheckResult(false, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
        }

        var overlappingBlock = await dbContext.Set<TimeBlock>()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => x.StartAtUtc < endAtUtc && x.EndAtUtc > normalizedStartAtUtc)
            .OrderBy(x => x.StartAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (overlappingBlock is not null)
        {
            reasons.Add($"Requested slot overlaps blocked time '{overlappingBlock.ReasonCode}'.");
            return new GroomerAvailabilityCheckResult(false, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
        }

        var hasOverlap = await appointmentOverlapReadService.HasOverlapAsync(groomerId, normalizedStartAtUtc, endAtUtc, ignoredAppointmentId, cancellationToken);
        if (hasOverlap)
        {
            reasons.Add("Requested slot overlaps an existing appointment.");
            return new GroomerAvailabilityCheckResult(false, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
        }

        reasons.Add("Requested slot is available.");
        return new GroomerAvailabilityCheckResult(true, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
    }

    public async Task<IReadOnlyCollection<AvailabilityWindowReadModel>> GetAvailabilityWindowsAsync(
        Guid groomerId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var groomerExists = await dbContext.Set<Groomer>()
            .AnyAsync(x => x.Id == groomerId && x.Active, cancellationToken);
        if (!groomerExists)
        {
            return [];
        }

        var timeZone = salonTimeZoneProvider.GetTimeZone();
        var localStart = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localEnd = DateTime.SpecifyKind(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

        var schedules = await dbContext.Set<WorkingSchedule>()
            .Where(x => x.GroomerId == groomerId)
            .OrderBy(x => x.Weekday)
            .ToListAsync(cancellationToken);

        var timeBlocks = await dbContext.Set<TimeBlock>()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => x.StartAtUtc < toUtc && x.EndAtUtc > fromUtc)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync(cancellationToken);

        return BuildAvailabilityWindows(fromUtc, toUtc, schedules, timeBlocks)
            .Select(x => new AvailabilityWindowReadModel(x.StartAtUtc.UtcDateTime, x.EndAtUtc.UtcDateTime))
            .ToArray();
    }

    private IReadOnlyCollection<AvailabilityWindowSegment> BuildAvailabilityWindows(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyCollection<WorkingSchedule> schedules,
        IReadOnlyCollection<TimeBlock> timeBlocks)
    {
        var timeZone = salonTimeZoneProvider.GetTimeZone();
        var windows = new List<AvailabilityWindowSegment>();
        var currentLocalDate = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, timeZone).Date;
        var endLocalDate = TimeZoneInfo.ConvertTimeFromUtc(toUtc, timeZone).Date;

        while (currentLocalDate <= endLocalDate)
        {
            var weekday = ToIsoWeekday(currentLocalDate.DayOfWeek);
            var schedule = schedules.SingleOrDefault(x => x.Weekday == weekday);
            if (schedule is not null)
            {
                var localWindowStart = DateTime.SpecifyKind(currentLocalDate.Add(schedule.StartLocalTime), DateTimeKind.Unspecified);
                var localWindowEnd = DateTime.SpecifyKind(currentLocalDate.Add(schedule.EndLocalTime), DateTimeKind.Unspecified);
                var windowStartUtc = TimeZoneInfo.ConvertTimeToUtc(localWindowStart, timeZone);
                var windowEndUtc = TimeZoneInfo.ConvertTimeToUtc(localWindowEnd, timeZone);

                if (windowEndUtc > fromUtc && windowStartUtc < toUtc)
                {
                    var clippedStart = windowStartUtc < fromUtc ? fromUtc : windowStartUtc;
                    var clippedEnd = windowEndUtc > toUtc ? toUtc : windowEndUtc;
                    AppendAvailabilitySegments(windows, clippedStart, clippedEnd, timeBlocks);
                }
            }

            currentLocalDate = currentLocalDate.AddDays(1);
        }

        return windows;
    }

    private bool IsInsideWorkingSchedule(DateTime startAtUtc, DateTime endAtUtc, IReadOnlyCollection<WorkingSchedule> schedules)
    {
        var timeZone = salonTimeZoneProvider.GetTimeZone();
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(startAtUtc, timeZone);
        var endLocal = TimeZoneInfo.ConvertTimeFromUtc(endAtUtc, timeZone);

        if (startLocal.Date != endLocal.Date)
        {
            return false;
        }

        var weekday = ToIsoWeekday(startLocal.DayOfWeek);
        var schedule = schedules.SingleOrDefault(x => x.Weekday == weekday);
        if (schedule is null)
        {
            return false;
        }

        var startTime = startLocal.TimeOfDay;
        var endTime = endLocal.TimeOfDay;
        return startTime >= schedule.StartLocalTime && endTime <= schedule.EndLocalTime;
    }

    private static void AppendAvailabilitySegments(
        List<AvailabilityWindowSegment> windows,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        IReadOnlyCollection<TimeBlock> timeBlocks)
    {
        var cursor = windowStartUtc;
        var overlappingBlocks = timeBlocks
            .Where(x => x.StartAtUtc < windowEndUtc && x.EndAtUtc > windowStartUtc)
            .OrderBy(x => x.StartAtUtc)
            .ToArray();

        foreach (var block in overlappingBlocks)
        {
            if (block.StartAtUtc > cursor)
            {
                windows.Add(new AvailabilityWindowSegment(cursor, block.StartAtUtc));
            }

            if (block.EndAtUtc > cursor)
            {
                cursor = block.EndAtUtc;
            }
        }

        if (cursor < windowEndUtc)
        {
            windows.Add(new AvailabilityWindowSegment(cursor, windowEndUtc));
        }
    }

    private static bool IsMatch(GroomerCapability capability, PetQuoteProfile pet, IReadOnlyCollection<Guid> offerIds)
    {
        if (capability.OfferId is not null && !offerIds.Contains(capability.OfferId.Value))
        {
            return false;
        }

        if (capability.AnimalTypeId is not null && capability.AnimalTypeId != pet.AnimalTypeId)
        {
            return false;
        }

        if (capability.BreedId is not null && capability.BreedId != pet.BreedId)
        {
            return false;
        }

        if (capability.BreedGroupId is not null && capability.BreedGroupId != pet.BreedGroupId)
        {
            return false;
        }

        if (capability.CoatTypeId is not null && capability.CoatTypeId != pet.CoatTypeId)
        {
            return false;
        }

        if (capability.SizeCategoryId is not null && capability.SizeCategoryId != pet.SizeCategoryId)
        {
            return false;
        }

        return true;
    }

    private static int ComputeSpecificity(GroomerCapability capability)
    {
        var score = 0;
        if (capability.BreedId is not null) score += 32;
        if (capability.BreedGroupId is not null) score += 16;
        if (capability.AnimalTypeId is not null) score += 8;
        if (capability.CoatTypeId is not null) score += 4;
        if (capability.SizeCategoryId is not null) score += 2;
        if (capability.OfferId is not null) score += 1;
        return score;
    }

    private static string BuildCapabilityReason(GroomerCapability capability, PetQuoteProfile pet, bool allow)
    {
        var subject = capability.BreedId is not null
            ? pet.BreedName
            : capability.BreedGroupId is not null
                ? pet.BreedGroupName ?? pet.AnimalTypeName
                : pet.AnimalTypeName;

        return allow
            ? $"Capability rule matched for {subject}."
            : $"Selected groomer cannot handle {subject} for the requested service composition.";
    }

    private static string BuildModifierReason(GroomerCapability capability, PetQuoteProfile pet)
    {
        var sign = capability.ReservedDurationModifierMinutes > 0 ? "+" : string.Empty;
        return $"Groomer capability modifier for {pet.BreedName}: {sign}{capability.ReservedDurationModifierMinutes} min.";
    }

    private static int ToIsoWeekday(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            _ => 7
        };
    }

    private sealed record AvailabilityWindowSegment(DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc);
}
