using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Staff.Infrastructure.Services;

public sealed class StaffSchedulingService(
    AppDbContext dbContext,
    IPetQuoteProfileService petQuoteProfileService,
    SalonTimeZoneProvider salonTimeZoneProvider,
    IAppointmentOverlapReadService appointmentOverlapReadService,
    IDistributedCache cache)
    : IStaffSchedulingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
        var groomerData = await LoadGroomerDataAsync(groomerId, cancellationToken);
        if (groomerData is null)
        {
            return Error.NotFound("Staff.GroomerNotFound", "Selected groomer does not exist or is inactive.");
        }

        var denial = groomerData.Capabilities
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
            var generalRule = groomerData.Capabilities
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
                var matched = groomerData.Capabilities
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
        DateTimeOffset startAt,
        int reservedMinutes,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(petId, cancellationToken);
        if (pet is null)
        {
            return Error.NotFound("Staff.PetNotFound", "Pet does not exist.");
        }

        return await CheckAvailabilityAsync(groomerId, pet, offerIds, startAt, reservedMinutes, ignoredAppointmentId, cancellationToken);
    }

    public async Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(
        Guid groomerId,
        PetQuoteProfile pet,
        IReadOnlyCollection<Guid> offerIds,
        DateTimeOffset startAt,
        int reservedMinutes,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        var normalizedStartAt = startAt.ToUniversalTime();
        var durationResolutionResult = await ResolveReservedDurationAsync(groomerId, pet, offerIds, reservedMinutes, cancellationToken);
        if (durationResolutionResult.IsError)
        {
            return durationResolutionResult.Errors;
        }

        var durationResolution = durationResolutionResult.Value;
        var endAt = normalizedStartAt.AddMinutes(durationResolution.EffectiveReservedMinutes);

        var reasons = new List<string>(durationResolution.Reasons);

        var groomerData = await LoadGroomerDataAsync(groomerId, cancellationToken);
        if (groomerData is null)
        {
            reasons.Add("Selected groomer does not exist or is inactive.");
            return new GroomerAvailabilityCheckResult(false, endAt, durationResolution.EffectiveReservedMinutes, reasons);
        }

        var schedule = await LoadWorkingSchedulesAsync(groomerId, cancellationToken);

        if (!IsInsideWorkingSchedule(normalizedStartAt, endAt, schedule))
        {
            reasons.Add("Requested slot is outside working schedule.");
            return new GroomerAvailabilityCheckResult(false, endAt, durationResolution.EffectiveReservedMinutes, reasons);
        }

        var overlappingBlock = await LoadOverlappingTimeBlockAsync(groomerId, normalizedStartAt, endAt, cancellationToken);

        if (overlappingBlock is not null)
        {
            reasons.Add($"Requested slot overlaps blocked time '{overlappingBlock.ReasonCode}'.");
            return new GroomerAvailabilityCheckResult(false, endAt, durationResolution.EffectiveReservedMinutes, reasons);
        }

        var hasOverlap = await appointmentOverlapReadService.HasOverlapAsync(groomerId, normalizedStartAt, endAt, ignoredAppointmentId, cancellationToken);
        if (hasOverlap)
        {
            reasons.Add("Requested slot overlaps an existing appointment.");
            return new GroomerAvailabilityCheckResult(false, endAt, durationResolution.EffectiveReservedMinutes, reasons);
        }

        reasons.Add("Requested slot is available.");
        return new GroomerAvailabilityCheckResult(true, endAt, durationResolution.EffectiveReservedMinutes, reasons);
    }

    public async Task<IReadOnlyCollection<AvailabilityWindowReadModel>> GetAvailabilityWindowsAsync(
        Guid groomerId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var groomerData = await LoadGroomerDataAsync(groomerId, cancellationToken);
        if (groomerData is null)
        {
            return [];
        }

        return await GetAvailabilityWindowsCoreAsync(groomerId, localDate, cancellationToken);
    }

    public async Task<ErrorOr<GroomerAvailableSlotsReadModel>> GetAvailableSlotsAsync(
        Guid groomerId,
        PetQuoteProfile pet,
        IReadOnlyCollection<Guid> offerIds,
        DateOnly localDate,
        int baseReservedMinutes,
        DateTimeOffset earliestStartAt,
        int slotStepMinutes,
        Guid? ignoredAppointmentId,
        CancellationToken cancellationToken)
    {
        if (slotStepMinutes <= 0)
        {
            return Error.Validation("Staff.SlotStepInvalid", "Slot step must be greater than zero minutes.");
        }

        var durationResult = await ResolveReservedDurationAsync(
            groomerId,
            pet,
            offerIds,
            baseReservedMinutes,
            cancellationToken);
        if (durationResult.IsError)
        {
            return durationResult.Errors;
        }

        var duration = durationResult.Value;
        var windows = await GetAvailabilityWindowsCoreAsync(groomerId, localDate, cancellationToken);
        if (windows.Count == 0)
        {
            return new GroomerAvailableSlotsReadModel(duration, []);
        }

        var slots = new List<AvailabilityWindowReadModel>();
        foreach (var window in windows)
        {
            foreach (var slotStartAt in GenerateSlotStarts(
                         window,
                         duration.EffectiveReservedMinutes,
                         earliestStartAt.ToUniversalTime(),
                         slotStepMinutes))
            {
                slots.Add(new AvailabilityWindowReadModel(
                    slotStartAt,
                    slotStartAt.AddMinutes(duration.EffectiveReservedMinutes)));
            }
        }

        if (slots.Count == 0)
        {
            return new GroomerAvailableSlotsReadModel(duration, []);
        }

        var busyIntervals = await appointmentOverlapReadService.ListBusyIntervalsAsync(
            groomerId,
            slots.Min(x => x.StartAt),
            slots.Max(x => x.EndAt),
            ignoredAppointmentId,
            cancellationToken);

        var availableSlots = slots
            .Where(slot => !busyIntervals.Any(busy => busy.StartAt < slot.EndAt && busy.EndAt > slot.StartAt))
            .ToArray();

        return new GroomerAvailableSlotsReadModel(duration, availableSlots);
    }

    private async Task<IReadOnlyCollection<AvailabilityWindowReadModel>> GetAvailabilityWindowsCoreAsync(
        Guid groomerId,
        DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var timeZone = salonTimeZoneProvider.GetTimeZone();
        var localStart = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localEnd = DateTime.SpecifyKind(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var from = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var to = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

        var schedules = await LoadWorkingSchedulesAsync(groomerId, cancellationToken);

        var timeBlocks = await dbContext.Set<TimeBlock>()
            .AsNoTracking()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => x.StartAt < to && x.EndAt > from)
            .OrderBy(x => x.StartAt)
            .Select(x => new CachedTimeBlockData(x.Id, x.GroomerId, x.StartAt, x.EndAt, x.ReasonCode))
            .ToListAsync(cancellationToken);

        return BuildAvailabilityWindows(from, to, schedules, timeBlocks)
            .Select(x => new AvailabilityWindowReadModel(x.StartAt, x.EndAt))
            .ToArray();
    }

    private static IReadOnlyCollection<DateTimeOffset> GenerateSlotStarts(
        AvailabilityWindowReadModel window,
        int reservedMinutes,
        DateTimeOffset earliestStartAt,
        int slotStepMinutes)
    {
        var starts = new List<DateTimeOffset>();
        var latestStartAt = window.EndAt.AddMinutes(-reservedMinutes);
        if (latestStartAt < window.StartAt)
        {
            return starts;
        }

        var cursor = window.StartAt > earliestStartAt ? window.StartAt : earliestStartAt;
        while (cursor <= latestStartAt)
        {
            starts.Add(cursor);
            cursor = cursor.AddMinutes(slotStepMinutes);
        }

        return starts;
    }

    // --- CACHE-AWARE DATA LOADERS ---

    private async Task<CachedGroomerData?> LoadGroomerDataAsync(Guid groomerId, CancellationToken cancellationToken)
    {
        var cacheKey = $"staff:groomer:{groomerId}:profile";
        var cached = await TryGetCachedAsync<CachedGroomerData>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var groomer = await dbContext.Set<Groomer>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == groomerId && x.Active, cancellationToken);
        if (groomer is null)
        {
            return null;
        }

        var capabilities = await dbContext.Set<GroomerCapability>()
            .AsNoTracking()
            .Where(x => x.GroomerId == groomer.Id)
            .Select(x => new CachedCapabilityData(
                x.Id, x.GroomerId, x.AnimalTypeId, x.BreedId, x.BreedGroupId,
                x.CoatTypeId, x.SizeCategoryId, x.OfferId, x.CapabilityMode,
                x.ReservedDurationModifierMinutes, x.Notes))
            .ToListAsync(cancellationToken);

        var data = new CachedGroomerData(groomer.Id, capabilities);
        await SetCachedAsync(cacheKey, data, cancellationToken);
        return data;
    }

    private async Task<List<CachedWorkingScheduleData>> LoadWorkingSchedulesAsync(Guid groomerId, CancellationToken cancellationToken)
    {
        var cacheKey = $"staff:groomer:{groomerId}:schedules";
        var cached = await TryGetCachedAsync<List<CachedWorkingScheduleData>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var schedules = await dbContext.Set<WorkingSchedule>()
            .AsNoTracking()
            .Where(x => x.GroomerId == groomerId)
            .OrderBy(x => x.Weekday)
            .Select(x => new CachedWorkingScheduleData(
                x.Id, x.GroomerId, x.Weekday, x.StartLocalTime, x.EndLocalTime))
            .ToListAsync(cancellationToken);

        await SetCachedAsync(cacheKey, schedules, cancellationToken);
        return schedules;
    }

    // --- CACHE HELPERS ---

    private async Task<T?> TryGetCachedAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        var data = await cache.GetStringAsync(key, cancellationToken);
        return data is null ? null : JsonSerializer.Deserialize<T>(data, JsonOptions);
    }

    private async Task SetCachedAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(value, JsonOptions);
        await cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);
    }

    private async Task<CachedTimeBlockData?> LoadOverlappingTimeBlockAsync(Guid groomerId, DateTimeOffset startAt, DateTimeOffset endAt, CancellationToken cancellationToken)
    {
        return await dbContext.Set<TimeBlock>()
            .AsNoTracking()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => x.StartAt < endAt && x.EndAt > startAt)
            .OrderBy(x => x.StartAt)
            .Select(x => new CachedTimeBlockData(x.Id, x.GroomerId, x.StartAt, x.EndAt, x.ReasonCode))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // --- EXISTING LOGIC (unchanged) ---

    private IReadOnlyCollection<AvailabilityWindowSegment> BuildAvailabilityWindows(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyCollection<CachedWorkingScheduleData> schedules,
        IReadOnlyCollection<CachedTimeBlockData> timeBlocks)
    {
        var timeZone = salonTimeZoneProvider.GetTimeZone();
        var windows = new List<AvailabilityWindowSegment>();
        var currentLocalDate = TimeZoneInfo.ConvertTime(from, timeZone).Date;
        var endLocalDate = TimeZoneInfo.ConvertTime(to, timeZone).Date;

        while (currentLocalDate <= endLocalDate)
        {
            var weekday = ToIsoWeekday(currentLocalDate.DayOfWeek);
            var schedule = schedules.SingleOrDefault(x => x.Weekday == weekday);
            if (schedule is not null)
            {
                var localWindowStart = DateTime.SpecifyKind(currentLocalDate.Add(schedule.StartLocalTime), DateTimeKind.Unspecified);
                var localWindowEnd = DateTime.SpecifyKind(currentLocalDate.Add(schedule.EndLocalTime), DateTimeKind.Unspecified);
                var windowStart = TimeZoneInfo.ConvertTimeToUtc(localWindowStart, timeZone);
                var windowEnd = TimeZoneInfo.ConvertTimeToUtc(localWindowEnd, timeZone);

                if (windowEnd > from && windowStart < to)
                {
                    var clippedStart = windowStart < from ? from : windowStart;
                    var clippedEnd = windowEnd > to ? to : windowEnd;
                    AppendAvailabilitySegments(windows, clippedStart, clippedEnd, timeBlocks);
                }
            }

            currentLocalDate = currentLocalDate.AddDays(1);
        }

        return windows;
    }

    private bool IsInsideWorkingSchedule(DateTimeOffset startAt, DateTimeOffset endAt, IReadOnlyCollection<CachedWorkingScheduleData> schedules)
    {
        var timeZone = salonTimeZoneProvider.GetTimeZone();
        var startLocal = TimeZoneInfo.ConvertTime(startAt, timeZone);
        var endLocal = TimeZoneInfo.ConvertTime(endAt, timeZone);

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
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        IReadOnlyCollection<CachedTimeBlockData> timeBlocks)
    {
        var cursor = windowStart;
        var overlappingBlocks = timeBlocks
            .Where(x => x.StartAt < windowEnd && x.EndAt > windowStart)
            .OrderBy(x => x.StartAt)
            .ToArray();

        foreach (var block in overlappingBlocks)
        {
            if (block.StartAt > cursor)
            {
                windows.Add(new AvailabilityWindowSegment(cursor, block.StartAt));
            }

            if (block.EndAt > cursor)
            {
                cursor = block.EndAt;
            }
        }

        if (cursor < windowEnd)
        {
            windows.Add(new AvailabilityWindowSegment(cursor, windowEnd));
        }
    }

    private static bool IsMatch(CachedCapabilityData capability, PetQuoteProfile pet, IReadOnlyCollection<Guid> offerIds)
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

    private static int ComputeSpecificity(CachedCapabilityData capability)
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

    private static string BuildCapabilityReason(CachedCapabilityData capability, PetQuoteProfile pet, bool allow)
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

    private static string BuildModifierReason(CachedCapabilityData capability, PetQuoteProfile pet)
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

    private sealed record AvailabilityWindowSegment(DateTimeOffset StartAt, DateTimeOffset EndAt);

    // --- CACHE DTOs ---

    internal sealed record CachedGroomerData(Guid Id, List<CachedCapabilityData> Capabilities);

    internal sealed record CachedCapabilityData(
        Guid Id, Guid GroomerId, Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId,
        Guid? CoatTypeId, Guid? SizeCategoryId, Guid? OfferId, string CapabilityMode,
        int ReservedDurationModifierMinutes, string? Notes);

    internal sealed record CachedWorkingScheduleData(
        Guid Id, Guid GroomerId, int Weekday, TimeSpan StartLocalTime, TimeSpan EndLocalTime);

    internal sealed record CachedTimeBlockData(
        Guid Id, Guid GroomerId, DateTimeOffset StartAt, DateTimeOffset EndAt, string? ReasonCode);
}
