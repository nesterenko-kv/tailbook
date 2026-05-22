using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class GroomerBookingReadService(
    AppDbContext dbContext,
    IGroomerProfileReadService groomerProfileReadService,
    IPetOperationalReadService petOperationalReadService,
    IVisitCatalogReadService visitCatalogReadService) : IGroomerBookingReadService
{
    public async Task<ErrorOr<PagedResult<GroomerAppointmentListItemView>>> ListAssignedAppointmentsAsync(Guid currentUserId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        var groomerResult = await groomerProfileReadService.GetByUserIdAsync(currentUserId, cancellationToken);
        if (groomerResult.IsError)
        {
            return groomerResult.Errors;
        }

        var groomer = groomerResult.Value;

        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var query = dbContext.Set<Appointment>().Where(x => x.GroomerId == groomer.GroomerId);
        // Read filters preserve the legacy API convention: compare local/unspecified inputs as UTC wall-clock values.
        if (from.HasValue)
        {
            query = query.Where(x => x.StartAt >= from.Value.ToUniversalTime());
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.StartAt < to.Value.ToUniversalTime());
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var appointments = await query
            .OrderBy(x => x.StartAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var appointmentIds = appointments.Select(x => x.Id).ToArray();
        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => appointmentIds.Contains(x.AppointmentId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var petCache = new Dictionary<Guid, PetOperationalReadModel?>();
        var resultItems = new List<GroomerAppointmentListItemView>();
        foreach (var appointment in appointments)
        {
            if (!petCache.TryGetValue(appointment.PetId, out PetOperationalReadModel? value))
            {
                value = await petOperationalReadService.GetPetOperationalAsync(appointment.PetId, cancellationToken);
                petCache[appointment.PetId] = value;
            }

            if (value is null)
            {
                return Error.Unexpected("Booking.AppointmentPetMissing", "Appointment pet does not exist.");
            }

            var pet = value;
            var appointmentItems = items.Where(x => x.AppointmentId == appointment.Id).ToArray();

            resultItems.Add(new GroomerAppointmentListItemView(
                appointment.Id,
                pet.Id,
                pet.Name,
                pet.BreedName,
                appointment.StartAt,
                appointment.EndAt,
                appointment.Status,
                appointmentItems.Sum(x => durationSnapshots.GetValueOrDefault(x.DurationSnapshotId)?.ReservedMinutes ?? 0),
                appointmentItems.Select(x => x.OfferDisplayNameSnapshot).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        return new PagedResult<GroomerAppointmentListItemView>(resultItems, safePage, safePageSize, totalCount);
    }

    public async Task<ErrorOr<GroomerAppointmentDetailView>> GetAssignedAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken)
    {
        var groomerResult = await groomerProfileReadService.GetByUserIdAsync(currentUserId, cancellationToken);
        if (groomerResult.IsError)
        {
            return groomerResult.Errors;
        }

        var groomer = groomerResult.Value;
        var appointment = await dbContext.Set<Appointment>()
            .SingleOrDefaultAsync(x => x.Id == appointmentId && x.GroomerId == groomer.GroomerId, cancellationToken);

        if (appointment is null)
        {
            return Error.NotFound("Booking.AppointmentNotFound", "Appointment does not exist.");
        }

        var pet = await petOperationalReadService.GetPetOperationalAsync(appointment.PetId, cancellationToken);
        if (pet is null)
        {
            return Error.Unexpected("Booking.AppointmentPetMissing", "Appointment pet does not exist.");
        }

        var items = await dbContext.Set<AppointmentItem>()
            .Where(x => x.AppointmentId == appointment.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var durationSnapshots = await dbContext.Set<DurationSnapshot>()
            .Where(x => items.Select(y => y.DurationSnapshotId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var itemViews = new List<GroomerAppointmentItemView>();
        foreach (var item in items)
        {
            var expectedComponents = await visitCatalogReadService.GetIncludedComponentsAsync(item.OfferVersionId, cancellationToken);
            itemViews.Add(new GroomerAppointmentItemView(
                item.Id,
                item.ItemType,
                item.OfferId,
                item.OfferVersionId,
                item.OfferCodeSnapshot,
                item.OfferDisplayNameSnapshot,
                item.Quantity,
                durationSnapshots.GetValueOrDefault(item.DurationSnapshotId)?.ServiceMinutes ?? 0,
                durationSnapshots.GetValueOrDefault(item.DurationSnapshotId)?.ReservedMinutes ?? 0,
                expectedComponents.Select(x => x.ProcedureName).ToArray()));
        }

        return new GroomerAppointmentDetailView(
            appointment.Id,
            new GroomerAppointmentPetView(pet.Id, pet.Name, pet.AnimalTypeCode, pet.AnimalTypeName, pet.BreedName, pet.CoatTypeCode, pet.SizeCategoryCode),
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status,
            itemViews.Sum(x => x.ReservedMinutes),
            string.IsNullOrWhiteSpace(pet.Notes) ? [] : [pet.Notes.Trim()],
            itemViews,
            appointment.CreatedAt,
            appointment.UpdatedAt);
    }
}
