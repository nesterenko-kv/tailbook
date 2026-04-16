using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Staff.Contracts;
using Tailbook.Modules.Staff.Domain;
using Tailbook.Modules.Staff.Infrastructure;

namespace Tailbook.Modules.Staff.Application;

public sealed class StaffSchedulingService(
    AppDbContext dbContext,
    IPetQuoteProfileService petQuoteProfileService,
    SalonTimeZoneProvider salonTimeZoneProvider)
    : IStaffSchedulingService
{
    public async Task<ReservedDurationResolution> ResolveReservedDurationAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        int baseReservedMinutes,
        CancellationToken cancellationToken)
    {
        var groomer = await dbContext.Set<Groomer>().SingleOrDefaultAsync(x => x.Id == groomerId && x.Active, cancellationToken)
            ?? throw new InvalidOperationException("Selected groomer does not exist or is inactive.");

        var pet = await petQuoteProfileService.GetPetAsync(petId, cancellationToken)
            ?? throw new InvalidOperationException("Pet does not exist.");

        var capabilities = await dbContext.Set<GroomerCapability>()
            .Where(x => x.GroomerId == groomer.Id)
            .ToListAsync(cancellationToken);

        var denial = capabilities
            .Where(x => IsMatch(x, pet, offerIds))
            .Where(x => string.Equals(x.CapabilityMode, CapabilityModeCodes.Deny, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => ComputeSpecificity(x))
            .FirstOrDefault();

        if (denial is not null)
        {
            var denialReason = BuildCapabilityReason(denial, pet, false);
            throw new InvalidOperationException(denialReason);
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
                .OrderByDescending(x => ComputeSpecificity(x))
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

    public async Task<GroomerAvailabilityCheckResult> CheckAvailabilityAsync(
        Guid groomerId,
        Guid petId,
        IReadOnlyCollection<Guid> offerIds,
        DateTime startAtUtc,
        int reservedMinutes,
        CancellationToken cancellationToken)
    {
        var durationResolution = await ResolveReservedDurationAsync(groomerId, petId, offerIds, reservedMinutes, cancellationToken);
        var endAtUtc = startAtUtc.AddMinutes(durationResolution.EffectiveReservedMinutes);

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

        if (!IsInsideWorkingSchedule(startAtUtc, endAtUtc, schedule))
        {
            reasons.Add("Requested slot is outside working schedule.");
            return new GroomerAvailabilityCheckResult(false, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
        }

        var overlappingBlock = await dbContext.Set<TimeBlock>()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => x.StartAtUtc < endAtUtc && x.EndAtUtc > startAtUtc)
            .OrderBy(x => x.StartAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (overlappingBlock is not null)
        {
            reasons.Add($"Requested slot overlaps blocked time '{overlappingBlock.ReasonCode}'.");
            return new GroomerAvailabilityCheckResult(false, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
        }

        reasons.Add("Requested slot is available.");
        return new GroomerAvailabilityCheckResult(true, endAtUtc, durationResolution.EffectiveReservedMinutes, reasons);
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
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }
}
