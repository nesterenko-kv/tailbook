using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Staff.Contracts;

namespace Tailbook.Modules.Staff.Infrastructure.Services;

public sealed class StaffUseCases(
    AppDbContext dbContext,
    IUserReferenceValidationService userReferenceValidationService,
    IOfferReferenceValidationService offerReferenceValidationService,
    IPetTaxonomyValidationService petTaxonomyValidationService,
    IStaffSchedulingService staffSchedulingService,
    SalonTimeZoneProvider salonTimeZoneProvider) : IStaffReadService
{
    public async Task<IReadOnlyCollection<GroomerListItemView>> ListGroomersAsync(CancellationToken cancellationToken)
    {
        var groomers = await dbContext.Set<Groomer>()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        var groomerIds = groomers.Select(x => x.Id).ToArray();
        var capabilityCounts = await dbContext.Set<GroomerCapability>()
            .Where(x => groomerIds.Contains(x.GroomerId))
            .GroupBy(x => x.GroomerId)
            .Select(x => new { GroomerId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.GroomerId, x => x.Count, cancellationToken);

        return groomers.Select(x => new GroomerListItemView(
            x.Id,
            x.UserId,
            x.DisplayName,
            x.Active,
            capabilityCounts.GetValueOrDefault(x.Id, 0),
            x.CreatedAtUtc,
            x.UpdatedAtUtc)).ToArray();
    }

    public async Task<GroomerDetailView?> GetGroomerAsync(Guid groomerId, CancellationToken cancellationToken)
    {
        var groomer = await dbContext.Set<Groomer>().SingleOrDefaultAsync(x => x.Id == groomerId, cancellationToken);
        if (groomer is null)
        {
            return null;
        }

        var capabilities = await dbContext.Set<GroomerCapability>()
            .Where(x => x.GroomerId == groomerId)
            .OrderByDescending(x => x.BreedId.HasValue)
            .ThenByDescending(x => x.BreedGroupId.HasValue)
            .ThenByDescending(x => x.AnimalTypeId.HasValue)
            .ThenByDescending(x => x.OfferId.HasValue)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var schedules = await dbContext.Set<WorkingSchedule>()
            .Where(x => x.GroomerId == groomerId)
            .OrderBy(x => x.Weekday)
            .ToListAsync(cancellationToken);

        return new GroomerDetailView(
            groomer.Id,
            groomer.UserId,
            groomer.DisplayName,
            groomer.Active,
            capabilities.Select(MapCapability).ToArray(),
            schedules.Select(MapSchedule).ToArray(),
            groomer.CreatedAtUtc,
            groomer.UpdatedAtUtc);
    }

    public async Task<ErrorOr<GroomerDetailView>> CreateGroomerAsync(string displayName, Guid? userId, CancellationToken cancellationToken)
    {
        var normalizedDisplayName = displayName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedDisplayName))
        {
            return Error.Validation("Staff.DisplayNameRequired", "Display name is required.");
        }

        if (userId is not null)
        {
            var userExists = await userReferenceValidationService.ExistsAsync(userId.Value, cancellationToken);
            if (!userExists)
            {
                return Error.NotFound("Staff.LinkedUserNotFound", "Linked IAM user does not exist.");
            }

            var duplicateUser = await dbContext.Set<Groomer>().AnyAsync(x => x.UserId == userId.Value, cancellationToken);
            if (duplicateUser)
            {
                return Error.Conflict("Staff.LinkedUserAlreadyAssigned", "The specified IAM user is already linked to another groomer.");
            }
        }

        var utcNow = DateTime.UtcNow;
        var entity = new Groomer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = normalizedDisplayName,
            Active = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<Groomer>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetGroomerAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ErrorOr<GroomerCapabilityView>> AddCapabilityAsync(AddGroomerCapabilityCommand command, CancellationToken cancellationToken)
    {
        var groomer = await dbContext.Set<Groomer>().SingleOrDefaultAsync(x => x.Id == command.GroomerId, cancellationToken);
        if (groomer is null)
        {
            return Error.NotFound("Staff.GroomerNotFound", "Groomer does not exist.");
        }

        var normalizedMode = NormalizeCapabilityMode(command.CapabilityMode);
        if (normalizedMode.IsError)
        {
            return normalizedMode.Errors;
        }

        if (command.AnimalTypeId is not null && !await petTaxonomyValidationService.AnimalTypeExistsAsync(command.AnimalTypeId.Value, cancellationToken))
        {
            return Error.NotFound("Staff.AnimalTypeNotFound", "Animal type does not exist.");
        }

        if (command.BreedId is not null && !await petTaxonomyValidationService.BreedExistsAsync(command.BreedId.Value, cancellationToken))
        {
            return Error.NotFound("Staff.BreedNotFound", "Breed does not exist.");
        }

        if (command.BreedGroupId is not null && !await petTaxonomyValidationService.BreedGroupExistsAsync(command.BreedGroupId.Value, cancellationToken))
        {
            return Error.NotFound("Staff.BreedGroupNotFound", "Breed group does not exist.");
        }

        if (command.CoatTypeId is not null && !await petTaxonomyValidationService.CoatTypeExistsAsync(command.CoatTypeId.Value, cancellationToken))
        {
            return Error.NotFound("Staff.CoatTypeNotFound", "Coat type does not exist.");
        }

        if (command.SizeCategoryId is not null && !await petTaxonomyValidationService.SizeCategoryExistsAsync(command.SizeCategoryId.Value, cancellationToken))
        {
            return Error.NotFound("Staff.SizeCategoryNotFound", "Size category does not exist.");
        }

        if (command.OfferId is not null && !await offerReferenceValidationService.ExistsAsync(command.OfferId.Value, cancellationToken))
        {
            return Error.NotFound("Staff.OfferNotFound", "Offer does not exist.");
        }

        var entity = new GroomerCapability
        {
            Id = Guid.NewGuid(),
            GroomerId = command.GroomerId,
            AnimalTypeId = command.AnimalTypeId,
            BreedId = command.BreedId,
            BreedGroupId = command.BreedGroupId,
            CoatTypeId = command.CoatTypeId,
            SizeCategoryId = command.SizeCategoryId,
            OfferId = command.OfferId,
            CapabilityMode = normalizedMode.Value,
            ReservedDurationModifierMinutes = command.ReservedDurationModifierMinutes,
            Notes = NormalizeOptional(command.Notes),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Set<GroomerCapability>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCapability(entity);
    }

    public async Task<ErrorOr<WorkingScheduleView>> UpsertWorkingScheduleAsync(Guid groomerId, int weekday, string startLocalTime, string endLocalTime, CancellationToken cancellationToken)
    {
        var groomerExists = await dbContext.Set<Groomer>().AnyAsync(x => x.Id == groomerId, cancellationToken);
        if (!groomerExists)
        {
            return Error.NotFound("Staff.GroomerNotFound", "Groomer does not exist.");
        }

        var normalizedWeekday = NormalizeWeekday(weekday);
        if (normalizedWeekday.IsError)
        {
            return normalizedWeekday.Errors;
        }

        var start = ParseLocalTime(startLocalTime, "startLocalTime");
        if (start.IsError)
        {
            return start.Errors;
        }

        var end = ParseLocalTime(endLocalTime, "endLocalTime");
        if (end.IsError)
        {
            return end.Errors;
        }

        if (end.Value <= start.Value)
        {
            return Error.Validation("Staff.InvalidWorkingScheduleRange", "Working schedule endLocalTime must be later than startLocalTime.");
        }

        var entity = await dbContext.Set<WorkingSchedule>()
            .SingleOrDefaultAsync(x => x.GroomerId == groomerId && x.Weekday == normalizedWeekday.Value, cancellationToken);

        if (entity is null)
        {
            entity = new WorkingSchedule
            {
                Id = Guid.NewGuid(),
                GroomerId = groomerId,
                Weekday = normalizedWeekday.Value,
                StartLocalTime = start.Value,
                EndLocalTime = end.Value,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Set<WorkingSchedule>().Add(entity);
        }
        else
        {
            entity.StartLocalTime = start.Value;
            entity.EndLocalTime = end.Value;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSchedule(entity);
    }

    public async Task<ErrorOr<TimeBlockView>> AddTimeBlockAsync(Guid groomerId, DateTime startAtUtc, DateTime endAtUtc, string reasonCode, string? notes, CancellationToken cancellationToken)
    {
        var groomerExists = await dbContext.Set<Groomer>().AnyAsync(x => x.Id == groomerId, cancellationToken);
        if (!groomerExists)
        {
            return Error.NotFound("Staff.GroomerNotFound", "Groomer does not exist.");
        }

        if (endAtUtc <= startAtUtc)
        {
            return Error.Validation("Staff.InvalidTimeBlockRange", "Time block endAtUtc must be later than startAtUtc.");
        }

        var normalizedReasonCode = NormalizeReasonCode(reasonCode);
        if (normalizedReasonCode.IsError)
        {
            return normalizedReasonCode.Errors;
        }

        var entity = new TimeBlock
        {
            Id = Guid.NewGuid(),
            GroomerId = groomerId,
            StartAtUtc = DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc),
            EndAtUtc = DateTime.SpecifyKind(endAtUtc, DateTimeKind.Utc),
            ReasonCode = normalizedReasonCode.Value,
            Notes = NormalizeOptional(notes),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Set<TimeBlock>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapTimeBlock(entity);
    }

    public async Task<ErrorOr<GroomerScheduleView>> GetScheduleAsync(Guid groomerId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var groomer = await dbContext.Set<Groomer>().SingleOrDefaultAsync(x => x.Id == groomerId, cancellationToken);
        if (groomer is null)
        {
            return Error.NotFound("Staff.GroomerNotFound", "Groomer does not exist.");
        }

        if (toUtc <= fromUtc)
        {
            return Error.Validation("Staff.InvalidScheduleRange", "Range end must be later than range start.");
        }

        var schedules = await dbContext.Set<WorkingSchedule>()
            .Where(x => x.GroomerId == groomerId)
            .OrderBy(x => x.Weekday)
            .ToListAsync(cancellationToken);

        var timeBlocks = await dbContext.Set<TimeBlock>()
            .Where(x => x.GroomerId == groomerId)
            .Where(x => x.StartAtUtc < toUtc && x.EndAtUtc > fromUtc)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync(cancellationToken);

        var windows = BuildAvailabilityWindows(fromUtc, toUtc, schedules, timeBlocks);

        return new GroomerScheduleView(
            groomer.Id,
            groomer.DisplayName,
            fromUtc,
            toUtc,
            schedules.Select(MapSchedule).ToArray(),
            timeBlocks.Select(MapTimeBlock).ToArray(),
            windows);
    }

    public async Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(CheckGroomerAvailabilityQuery query, CancellationToken cancellationToken)
    {
        return await staffSchedulingService.CheckAvailabilityAsync(
            query.GroomerId,
            query.PetId,
            query.OfferIds,
            query.StartAtUtc,
            query.ReservedMinutes,
            null,
            cancellationToken);
    }

    private IReadOnlyCollection<AvailabilityWindowView> BuildAvailabilityWindows(DateTimeOffset fromUtc, DateTimeOffset toUtc, IReadOnlyCollection<WorkingSchedule> schedules, IReadOnlyCollection<TimeBlock> timeBlocks)
    {
        var timeZone = salonTimeZoneProvider.GetTimeZone();
        var windows = new List<AvailabilityWindowView>();
        var currentLocalDate = TimeZoneInfo.ConvertTime(fromUtc, timeZone).Date;
        var endLocalDate = TimeZoneInfo.ConvertTime(toUtc, timeZone).Date;

        while (currentLocalDate <= endLocalDate)
        {
            var weekday = ToIsoWeekday(currentLocalDate.DayOfWeek);
            var schedule = schedules.SingleOrDefault(x => x.Weekday == weekday);
            if (schedule is not null)
            {
                var localStart = DateTime.SpecifyKind(currentLocalDate.Add(schedule.StartLocalTime), DateTimeKind.Unspecified);
                var localEnd = DateTime.SpecifyKind(currentLocalDate.Add(schedule.EndLocalTime), DateTimeKind.Unspecified);
                var windowStartUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
                var windowEndUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

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

    private static void AppendAvailabilitySegments(List<AvailabilityWindowView> windows, DateTimeOffset windowStartUtc, DateTimeOffset windowEndUtc, IReadOnlyCollection<TimeBlock> timeBlocks)
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
                windows.Add(new AvailabilityWindowView(cursor, block.StartAtUtc));
            }

            if (block.EndAtUtc > cursor)
            {
                cursor = block.EndAtUtc;
            }
        }

        if (cursor < windowEndUtc)
        {
            windows.Add(new AvailabilityWindowView(cursor, windowEndUtc));
        }
    }

    private static GroomerCapabilityView MapCapability(GroomerCapability x)
        => new(x.Id, x.GroomerId, x.AnimalTypeId, x.BreedId, x.BreedGroupId, x.CoatTypeId, x.SizeCategoryId, x.OfferId, x.CapabilityMode, x.ReservedDurationModifierMinutes, x.Notes, x.CreatedAtUtc);

    private static WorkingScheduleView MapSchedule(WorkingSchedule x)
        => new(x.Id, x.GroomerId, x.Weekday, FormatLocalTime(x.StartLocalTime), FormatLocalTime(x.EndLocalTime), x.CreatedAtUtc, x.UpdatedAtUtc);

    private static TimeBlockView MapTimeBlock(TimeBlock x)
        => new(x.Id, x.GroomerId, x.StartAtUtc, x.EndAtUtc, x.ReasonCode, x.Notes, x.CreatedAtUtc);

    private static ErrorOr<string> NormalizeCapabilityMode(string capabilityMode)
    {
        var normalized = capabilityMode.Trim();
        var match = CapabilityModeCodes.All.SingleOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? Error.Validation("Staff.UnknownCapabilityMode", $"Unknown capability mode '{capabilityMode}'.")
            : match;
    }

    private static ErrorOr<int> NormalizeWeekday(int weekday)
    {
        if (weekday is < 1 or > 7)
        {
            return Error.Validation("Staff.InvalidWeekday", "Weekday must be between 1 and 7.");
        }

        return weekday;
    }

    private static ErrorOr<TimeSpan> ParseLocalTime(string value, string fieldName)
    {
        if (!TimeSpan.TryParse(value, out var result))
        {
            return Error.Validation($"Staff.Invalid{fieldName}", $"{fieldName} must be a valid local time in HH:mm format.");
        }

        return new TimeSpan(result.Hours, result.Minutes, 0);
    }

    private static string FormatLocalTime(TimeSpan value) => value.ToString(@"hh\:mm");

    private static ErrorOr<string> NormalizeReasonCode(string value)
    {
        var normalized = value.Trim().ToUpperInvariant().Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Error.Validation("Staff.ReasonCodeRequired", "Reason code is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int ToIsoWeekday(DayOfWeek dayOfWeek)
    {
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }
}
