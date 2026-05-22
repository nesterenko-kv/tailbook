using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Search;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services;

public sealed class VisitReadService(
    AppDbContext dbContext,
    IAppointmentVisitService appointmentVisitService,
    IVisitCatalogReadService visitCatalogReadService,
    IPetSummaryReadService petSummaryReadService,
    IAccessAuditService accessAuditService) : IVisitReadService
{
    public async Task<VisitDetailView?> GetVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken, bool recordAccessAudit = true)
    {
        var result = await GetVisitResultAsync(visitId, actorUserId, cancellationToken, recordAccessAudit);
        return result.IsError ? null : result.Value;
    }

    public async Task<ErrorOr<PagedResult<VisitListItemView>>> ListVisitsAsync(
        string? search,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? groomerId,
        Guid? appointmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };
        var normalizedStatus = NormalizeOptional(status);
        var searchTerms = SearchText.Terms(search);

        if (!string.IsNullOrWhiteSpace(normalizedStatus) && !VisitStatusCodes.All.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase))
        {
            return Error.Validation("VisitOperations.UnknownVisitStatus", $"Unknown visit status '{status}'.");
        }
        var canonicalStatus = string.IsNullOrWhiteSpace(normalizedStatus)
            ? null
            : VisitStatusCodes.All.Single(x => string.Equals(x, normalizedStatus, StringComparison.OrdinalIgnoreCase));

        var query = dbContext.Set<Visit>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(canonicalStatus))
        {
            query = query.Where(x => x.Status == canonicalStatus);
        }

        if (appointmentId.HasValue)
        {
            query = query.Where(x => x.AppointmentId == appointmentId.Value);
        }

        var candidateVisits = await query
            .OrderByDescending(x => x.CheckedInAt)
            .ToListAsync(cancellationToken);
        var appointments = await appointmentVisitService.ListAppointmentsAsync(
            candidateVisits.Select(x => x.AppointmentId).ToArray(),
            from.HasValue ? from.Value.ToUniversalTime() : null,
            to.HasValue ? to.Value.ToUniversalTime() : null,
            groomerId,
            cancellationToken);
        var petIdsBySearchTerm = await SearchPetIdsByTermAsync(searchTerms, cancellationToken);

        var filteredVisits = candidateVisits
            .Where(x => appointments.TryGetValue(x.AppointmentId, out var appointment) &&
                        MatchesSearch(x, appointment, searchTerms, petIdsBySearchTerm))
            .ToArray();
        var totalCount = filteredVisits.Length;
        var pageVisits = filteredVisits
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToArray();

        var items = new List<VisitListItemView>();
        foreach (var visit in pageVisits)
        {
            var appointment = appointments[visit.AppointmentId];
            var pet = await petSummaryReadService.GetPetSummaryAsync(appointment.PetId, cancellationToken);
            if (pet is null)
            {
                return Error.Unexpected("VisitOperations.VisitPetMissing", "Visit pet does not exist.");
            }
            var appointmentTotal = appointment.Items.Sum(x => x.PriceAmount * x.Quantity);
            var adjustmentTotal = await dbContext.Set<VisitPriceAdjustment>()
                .Where(x => x.VisitId == visit.Id)
                .SumAsync(x => x.Amount * x.Sign, cancellationToken);

            items.Add(new VisitListItemView(
                visit.Id,
                visit.AppointmentId,
                appointment.BookingRequestId,
                appointment.PetId,
                pet.Name,
                pet.BreedName,
                appointment.GroomerId,
                appointment.StartAt,
                appointment.EndAt,
                visit.Status,
                visit.CheckedInAt,
                visit.StartedAt,
                visit.CompletedAt,
                visit.ClosedAt,
                appointment.Items.Count,
                appointmentTotal,
                adjustmentTotal,
                appointmentTotal + adjustmentTotal));
        }

        return new PagedResult<VisitListItemView>(items, safePage, safePageSize, totalCount);
    }

    private async Task<ErrorOr<VisitDetailView>> GetVisitResultAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken, bool recordAccessAudit = true)
    {
        var visit = await dbContext.Set<Visit>().SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);
        if (visit is null)
        {
            return Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.");
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(visit.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Error.Unexpected("VisitOperations.VisitAppointmentMissing", "Visit appointment does not exist.");
        }

        var pet = await petSummaryReadService.GetPetSummaryAsync(appointment.PetId, cancellationToken);
        if (pet is null)
        {
            return Error.Unexpected("VisitOperations.VisitPetMissing", "Visit pet does not exist.");
        }

        var executionItems = await dbContext.Set<VisitExecutionItem>()
            .Where(x => x.VisitId == visitId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var performed = await dbContext.Set<VisitPerformedProcedure>()
            .Where(x => executionItems.Select(y => y.Id).Contains(x.VisitExecutionItemId))
            .OrderBy(x => x.RecordedAt)
            .ToListAsync(cancellationToken);

        var skipped = await dbContext.Set<VisitSkippedComponent>()
            .Where(x => executionItems.Select(y => y.Id).Contains(x.VisitExecutionItemId))
            .OrderBy(x => x.RecordedAt)
            .ToListAsync(cancellationToken);

        var adjustments = await dbContext.Set<VisitPriceAdjustment>()
            .Where(x => x.VisitId == visitId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var componentMap = new Dictionary<Guid, IReadOnlyCollection<OfferExecutionComponentInfo>>();
        foreach (var executionItem in executionItems)
        {
            componentMap[executionItem.Id] = await visitCatalogReadService.GetIncludedComponentsAsync(executionItem.OfferVersionId, cancellationToken);
        }

        if (recordAccessAudit && actorUserId.HasValue)
        {
            await accessAuditService.RecordAsync("visit", visitId.ToString("D"), "READ_VISIT_DETAIL", actorUserId, cancellationToken);
        }

        var appointmentTotal = executionItems.Sum(x => x.PriceAmountSnapshot * x.Quantity);
        var adjustmentTotal = adjustments.Sum(x => x.Amount * x.Sign);

        return new VisitDetailView(
            visit.Id,
            visit.AppointmentId,
            appointment.BookingRequestId,
            new VisitPetView(pet.Id, pet.Name, pet.AnimalTypeCode, pet.AnimalTypeName, pet.BreedName, pet.CoatTypeCode, pet.SizeCategoryCode),
            appointment.GroomerId,
            visit.Status,
            visit.CheckedInAt,
            visit.StartedAt,
            visit.CompletedAt,
            visit.ClosedAt,
            executionItems.Sum(x => x.ServiceMinutesSnapshot * x.Quantity),
            executionItems.Sum(x => x.ReservedMinutesSnapshot * x.Quantity),
            appointmentTotal,
            adjustmentTotal,
            appointmentTotal + adjustmentTotal,
            executionItems.Select(item => new VisitExecutionItemView(
                item.Id,
                item.AppointmentItemId,
                item.ItemType,
                item.OfferId,
                item.OfferVersionId,
                item.OfferCodeSnapshot,
                item.OfferDisplayNameSnapshot,
                item.Quantity,
                item.PriceAmountSnapshot,
                item.ServiceMinutesSnapshot,
                item.ReservedMinutesSnapshot,
                componentMap[item.Id].Select(component => new VisitExpectedComponentView(
                    component.Id,
                    component.ProcedureId,
                    component.ProcedureCode,
                    component.ProcedureName,
                    component.ComponentRole,
                    component.SequenceNo,
                    component.DefaultExpected,
                    skipped.Any(x => x.VisitExecutionItemId == item.Id && x.OfferVersionComponentId == component.Id))).ToArray(),
                performed.Where(x => x.VisitExecutionItemId == item.Id)
                    .Select(x => new VisitPerformedProcedureView(x.Id, x.ProcedureId, x.ProcedureCodeSnapshot, x.ProcedureNameSnapshot, x.Status, x.Note, x.RecordedAt)).ToArray(),
                skipped.Where(x => x.VisitExecutionItemId == item.Id)
                    .Select(x => new VisitSkippedComponentView(x.Id, x.OfferVersionComponentId, x.ProcedureId, x.ProcedureCodeSnapshot, x.ProcedureNameSnapshot, x.OmissionReasonCode, x.Note, x.RecordedAt)).ToArray()))
                .ToArray(),
            adjustments.Select(x => new VisitPriceAdjustmentView(x.Id, x.Sign, x.Amount, x.ReasonCode, x.Note, x.CreatedAt)).ToArray(),
            visit.CreatedAt,
            visit.UpdatedAt);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Dictionary<string, Guid[]>> SearchPetIdsByTermAsync(string[] searchTerms, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Guid[]>(StringComparer.Ordinal);
        foreach (var term in searchTerms)
        {
            result[term] = (await petSummaryReadService.SearchPetIdsAsync(term, 1000, cancellationToken)).ToArray();
        }

        return result;
    }

    private static bool MatchesSearch(
        Visit visit,
        VisitAppointmentInfo appointment,
        IReadOnlyCollection<string> searchTerms,
        IReadOnlyDictionary<string, Guid[]> petIdsBySearchTerm)
    {
        if (searchTerms.Count == 0)
        {
            return true;
        }

        foreach (var term in searchTerms)
        {
            var matchingPetIds = petIdsBySearchTerm.GetValueOrDefault(term) ?? [];
            if (matchingPetIds.Contains(appointment.PetId) ||
                SearchText.ContainsTerm(term, visit.Status, appointment.Status) ||
                appointment.Items.Any(item => SearchText.ContainsTerm(term, item.ItemType, item.OfferCode, item.OfferDisplayName)))
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
