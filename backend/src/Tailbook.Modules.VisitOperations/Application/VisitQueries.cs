using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations.Contracts;
using Tailbook.Modules.VisitOperations.Domain;

namespace Tailbook.Modules.VisitOperations.Application;

public sealed class VisitQueries(
    AppDbContext dbContext,
    IAppointmentVisitService appointmentVisitService,
    IVisitCatalogReadService visitCatalogReadService,
    IPetSummaryReadService petSummaryReadService,
    IAccessAuditService accessAuditService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher)
{
    public async Task<ErrorOr<VisitDetailView>> CheckInAppointmentAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var existingVisit = await dbContext.Set<Visit>().SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        if (existingVisit is not null)
        {
            return Error.Conflict("VisitOperations.AppointmentAlreadyCheckedIn", "Appointment has already been checked in.");
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            return Error.NotFound("VisitOperations.AppointmentNotFound", "Appointment does not exist.");
        }

        try
        {
            await appointmentVisitService.MarkCheckedInAsync(appointmentId, actorUserId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict("VisitOperations.AppointmentCheckInFailed", ex.Message);
        }

        var utcNow = DateTime.UtcNow;
        var visit = Visit.CheckIn(
            Guid.NewGuid(),
            appointmentId,
            appointment.Items.Select(x => new VisitExecutionItemDraft(
                x.AppointmentItemId,
                x.ItemType,
                x.OfferId,
                x.OfferVersionId,
                x.OfferCode,
                x.OfferDisplayName,
                x.Quantity,
                x.PriceAmount,
                x.ServiceMinutes,
                x.ReservedMinutes)).ToArray(),
            actorUserId,
            utcNow);

        dbContext.Set<Visit>().Add(visit);

        await outboxPublisher.PublishAsync("visitops", "VisitCheckedIn", new
        {
            visitId = visit.Id,
            appointmentId = visit.AppointmentId,
            status = visit.Status,
            checkedInAtUtc = visit.CheckedInAtUtc
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("visitops", "visit", visit.Id.ToString("D"), "CHECK_IN", actorUserId, null, System.Text.Json.JsonSerializer.Serialize(new { visit.Status }), cancellationToken);
        return (await GetVisitAsync(visit.Id, actorUserId, cancellationToken))!;
    }

    public async Task<VisitDetailView?> GetVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken, bool recordAccessAudit = true)
    {
        var visit = await dbContext.Set<Visit>().SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);
        if (visit is null)
        {
            return null;
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(visit.AppointmentId, cancellationToken)
            ?? throw new InvalidOperationException("Visit appointment does not exist.");

        var pet = await petSummaryReadService.GetPetSummaryAsync(appointment.PetId, cancellationToken)
            ?? throw new InvalidOperationException("Visit pet does not exist.");

        var executionItems = await dbContext.Set<VisitExecutionItem>()
            .Where(x => x.VisitId == visitId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var performed = await dbContext.Set<VisitPerformedProcedure>()
            .Where(x => executionItems.Select(y => y.Id).Contains(x.VisitExecutionItemId))
            .OrderBy(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        var skipped = await dbContext.Set<VisitSkippedComponent>()
            .Where(x => executionItems.Select(y => y.Id).Contains(x.VisitExecutionItemId))
            .OrderBy(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        var adjustments = await dbContext.Set<VisitPriceAdjustment>()
            .Where(x => x.VisitId == visitId)
            .OrderBy(x => x.CreatedAtUtc)
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
            visit.CheckedInAtUtc,
            visit.StartedAtUtc,
            visit.CompletedAtUtc,
            visit.ClosedAtUtc,
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
                    .Select(x => new VisitPerformedProcedureView(x.Id, x.ProcedureId, x.ProcedureCodeSnapshot, x.ProcedureNameSnapshot, x.Status, x.Note, x.RecordedAtUtc)).ToArray(),
                skipped.Where(x => x.VisitExecutionItemId == item.Id)
                    .Select(x => new VisitSkippedComponentView(x.Id, x.OfferVersionComponentId, x.ProcedureId, x.ProcedureCodeSnapshot, x.ProcedureNameSnapshot, x.OmissionReasonCode, x.Note, x.RecordedAtUtc)).ToArray()))
                .ToArray(),
            adjustments.Select(x => new VisitPriceAdjustmentView(x.Id, x.Sign, x.Amount, x.ReasonCode, x.Note, x.CreatedAtUtc)).ToArray(),
            visit.CreatedAtUtc,
            visit.UpdatedAtUtc);
    }

    public async Task<ErrorOr<PagedResult<VisitListItemView>>> ListVisitsAsync(
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? groomerId,
        Guid? appointmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };
        var normalizedStatus = NormalizeOptional(status);

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
            .OrderByDescending(x => x.CheckedInAtUtc)
            .ToListAsync(cancellationToken);
        var appointments = await appointmentVisitService.ListAppointmentsAsync(
            candidateVisits.Select(x => x.AppointmentId).ToArray(),
            fromUtc.HasValue ? DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc) : null,
            toUtc.HasValue ? DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc) : null,
            groomerId,
            cancellationToken);

        var filteredVisits = candidateVisits
            .Where(x => appointments.ContainsKey(x.AppointmentId))
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
            var pet = await petSummaryReadService.GetPetSummaryAsync(appointment.PetId, cancellationToken)
                ?? throw new InvalidOperationException("Visit pet does not exist.");
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
                appointment.StartAtUtc,
                appointment.EndAtUtc,
                visit.Status,
                visit.CheckedInAtUtc,
                visit.StartedAtUtc,
                visit.CompletedAtUtc,
                visit.ClosedAtUtc,
                appointment.Items.Count,
                appointmentTotal,
                adjustmentTotal,
                appointmentTotal + adjustmentTotal));
        }

        return new PagedResult<VisitListItemView>(items, safePage, safePageSize, totalCount);
    }

    public async Task<ErrorOr<VisitDetailView>> RecordPerformedProcedureAsync(Guid visitId, Guid visitExecutionItemId, Guid procedureId, string? note, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAggregateAsync(visitId, cancellationToken);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        var wasOpen = visit.Value.Status == VisitStatusCodes.Open;

        var procedure = await visitCatalogReadService.GetProcedureAsync(procedureId, cancellationToken);
        if (procedure is null)
        {
            return Error.NotFound("VisitOperations.ProcedureNotFound", "Procedure does not exist.");
        }

        VisitPerformedProcedure performedProcedure;
        try
        {
            performedProcedure = visit.Value.RecordPerformedProcedure(
                visitExecutionItemId,
                new VisitPerformedProcedureDraft(
                    procedure.Id,
                    procedure.Code,
                    procedure.Name,
                    note),
                actorUserId,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("VisitOperations.RecordProcedureFailed", ex.Message);
        }
        dbContext.Set<VisitPerformedProcedure>().Add(performedProcedure);

        if (wasOpen && visit.Value.Status == VisitStatusCodes.InProgress)
        {
            await appointmentVisitService.MarkInProgressAsync(visit.Value.AppointmentId, actorUserId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<ErrorOr<VisitDetailView>> RecordSkippedComponentAsync(Guid visitId, Guid visitExecutionItemId, Guid offerVersionComponentId, string omissionReasonCode, string? note, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAggregateAsync(visitId, cancellationToken);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        var wasOpen = visit.Value.Status == VisitStatusCodes.Open;
        VisitExecutionItem executionItem;
        try
        {
            executionItem = visit.Value.GetExecutionItem(visitExecutionItemId);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("VisitOperations.ExecutionItemNotFound", ex.Message);
        }

        var component = await visitCatalogReadService.GetComponentAsync(offerVersionComponentId, cancellationToken);
        if (component is null)
        {
            return Error.NotFound("VisitOperations.ComponentNotFound", "Offer version component does not exist.");
        }

        if (component.OfferVersionId != executionItem.OfferVersionId)
        {
            return Error.Validation("VisitOperations.ComponentExecutionItemMismatch", "Selected component does not belong to this visit execution item.");
        }

        if (!string.Equals(component.ComponentRole, "Included", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("VisitOperations.ComponentNotIncluded", "Only included components can be marked as skipped.");
        }

        VisitSkippedComponent skippedComponent;
        try
        {
            skippedComponent = visit.Value.RecordSkippedComponent(
                visitExecutionItemId,
                new VisitSkippedComponentDraft(
                    component.Id,
                    component.ProcedureId,
                    component.ProcedureCode,
                    component.ProcedureName,
                    omissionReasonCode,
                    note),
                actorUserId,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("VisitOperations.RecordSkippedComponentFailed", ex.Message);
        }
        dbContext.Set<VisitSkippedComponent>().Add(skippedComponent);

        if (wasOpen && visit.Value.Status == VisitStatusCodes.InProgress)
        {
            await appointmentVisitService.MarkInProgressAsync(visit.Value.AppointmentId, actorUserId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<ErrorOr<VisitDetailView>> ApplyPriceAdjustmentAsync(Guid visitId, int sign, decimal amount, string reasonCode, string? note, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAggregateAsync(visitId, cancellationToken);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        VisitPriceAdjustment adjustment;
        try
        {
            adjustment = visit.Value.ApplyPriceAdjustment(
                new VisitPriceAdjustmentDraft(sign, amount, reasonCode, note),
                actorUserId,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("VisitOperations.ApplyAdjustmentFailed", ex.Message);
        }
        dbContext.Set<VisitPriceAdjustment>().Add(adjustment);

        await outboxPublisher.PublishAsync("visitops", "FinalPriceAdjusted", new
        {
            visitId = visit.Value.Id,
            status = visit.Value.Status,
            sign = adjustment.Sign,
            amount = adjustment.Amount,
            reasonCode = adjustment.ReasonCode
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("visitops", "visit", visit.Value.Id.ToString("D"), "APPLY_ADJUSTMENT", actorUserId, null, System.Text.Json.JsonSerializer.Serialize(new { adjustment.Sign, adjustment.Amount, adjustment.ReasonCode }), cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<ErrorOr<VisitDetailView>> CompleteVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAggregateAsync(visitId, cancellationToken);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        try
        {
            visit.Value.EnsureCanBeCompleted();
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict("VisitOperations.VisitCompletionFailed", ex.Message);
        }

        var componentsAccountedFor = await EnsureDefaultExpectedComponentsAccountedForAsync(visit.Value.Id, cancellationToken);
        if (componentsAccountedFor.IsError)
        {
            return componentsAccountedFor.Errors;
        }

        visit.Value.Complete(actorUserId, DateTime.UtcNow);

        await appointmentVisitService.MarkCompletedAsync(visit.Value.AppointmentId, actorUserId, cancellationToken);
        await outboxPublisher.PublishAsync("visitops", "VisitCompleted", new
        {
            visitId = visit.Value.Id,
            appointmentId = visit.Value.AppointmentId,
            status = visit.Value.Status,
            completedAtUtc = visit.Value.CompletedAtUtc
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("visitops", "visit", visit.Value.Id.ToString("D"), "COMPLETE", actorUserId, null, System.Text.Json.JsonSerializer.Serialize(new { visit.Value.Status, visit.Value.CompletedAtUtc }), cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<ErrorOr<VisitDetailView>> CloseVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAggregateAsync(visitId, cancellationToken);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        try
        {
            visit.Value.Close(actorUserId, DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Conflict("VisitOperations.VisitCloseFailed", ex.Message);
        }

        await appointmentVisitService.MarkClosedAsync(visit.Value.AppointmentId, actorUserId, cancellationToken);
        await outboxPublisher.PublishAsync("visitops", "VisitClosed", new
        {
            visitId = visit.Value.Id,
            appointmentId = visit.Value.AppointmentId,
            status = visit.Value.Status,
            finalTotalAmount = visit.Value.FinalTotalAmount,
            closedAtUtc = visit.Value.ClosedAtUtc
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrailService.RecordAsync("visitops", "visit", visit.Value.Id.ToString("D"), "CLOSE", actorUserId, null, System.Text.Json.JsonSerializer.Serialize(new { visit.Value.Status, visit.Value.FinalTotalAmount }), cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    private async Task<ErrorOr<Visit>> LoadVisitAggregateAsync(Guid visitId, CancellationToken cancellationToken)
    {
        var visit = await dbContext.Set<Visit>()
                   .Include(x => x.ExecutionItems)
                   .ThenInclude(x => x.PerformedProcedures)
                   .Include(x => x.ExecutionItems)
                   .ThenInclude(x => x.SkippedComponents)
                   .Include(x => x.PriceAdjustments)
                   .SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken)
               ;
        return visit is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : visit;
    }

    private async Task<ErrorOr<bool>> EnsureDefaultExpectedComponentsAccountedForAsync(Guid visitId, CancellationToken cancellationToken)
    {
        var executionItems = await dbContext.Set<VisitExecutionItem>()
            .Where(x => x.VisitId == visitId)
            .ToListAsync(cancellationToken);
        var executionItemIds = executionItems.Select(x => x.Id).ToArray();
        var performedProcedures = await dbContext.Set<VisitPerformedProcedure>()
            .Where(x => executionItemIds.Contains(x.VisitExecutionItemId))
            .ToListAsync(cancellationToken);
        var performedProcedureIdsByItem = performedProcedures
            .GroupBy(x => x.VisitExecutionItemId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ProcedureId).ToHashSet());
        var skippedComponents = await dbContext.Set<VisitSkippedComponent>()
            .Where(x => executionItemIds.Contains(x.VisitExecutionItemId))
            .ToListAsync(cancellationToken);
        var skippedComponentIdsByItem = skippedComponents
            .GroupBy(x => x.VisitExecutionItemId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.OfferVersionComponentId).ToHashSet());

        foreach (var executionItem in executionItems)
        {
            var components = await visitCatalogReadService.GetIncludedComponentsAsync(executionItem.OfferVersionId, cancellationToken);
            foreach (var component in components.Where(x => x.DefaultExpected))
            {
                var wasPerformed = performedProcedureIdsByItem.TryGetValue(executionItem.Id, out var performedProcedureIds)
                                   && performedProcedureIds.Contains(component.ProcedureId);
                var wasSkipped = skippedComponentIdsByItem.TryGetValue(executionItem.Id, out var skippedComponentIds)
                                 && skippedComponentIds.Contains(component.Id);
                if (!wasPerformed && !wasSkipped)
                {
                    return Error.Validation("VisitOperations.DefaultComponentIncomplete", $"Default expected component '{component.ProcedureName}' must be performed or skipped before completion.");
                }
            }
        }

        return true;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record VisitPetView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode);
public sealed record VisitExpectedComponentView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string ComponentRole, int SequenceNo, bool DefaultExpected, bool IsSkipped);
public sealed record VisitPerformedProcedureView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string Status, string? Note, DateTime RecordedAtUtc);
public sealed record VisitSkippedComponentView(Guid Id, Guid OfferVersionComponentId, Guid ProcedureId, string ProcedureCode, string ProcedureName, string OmissionReasonCode, string? Note, DateTime RecordedAtUtc);
public sealed record VisitExecutionItemView(Guid Id, Guid AppointmentItemId, string ItemType, Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferDisplayName, int Quantity, decimal PriceAmount, int ServiceMinutes, int ReservedMinutes, IReadOnlyCollection<VisitExpectedComponentView> ExpectedComponents, IReadOnlyCollection<VisitPerformedProcedureView> PerformedProcedures, IReadOnlyCollection<VisitSkippedComponentView> SkippedComponents);
public sealed record VisitPriceAdjustmentView(Guid Id, int Sign, decimal Amount, string ReasonCode, string? Note, DateTime CreatedAtUtc);
public sealed record VisitDetailView(Guid Id, Guid AppointmentId, Guid? BookingRequestId, VisitPetView Pet, Guid GroomerId, string Status, DateTime CheckedInAtUtc, DateTime? StartedAtUtc, DateTime? CompletedAtUtc, DateTime? ClosedAtUtc, int ServiceMinutes, int ReservedMinutes, decimal AppointmentTotalAmount, decimal AdjustmentTotalAmount, decimal FinalTotalAmount, IReadOnlyCollection<VisitExecutionItemView> Items, IReadOnlyCollection<VisitPriceAdjustmentView> Adjustments, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record VisitListItemView(Guid Id, Guid AppointmentId, Guid? BookingRequestId, Guid PetId, string PetName, string BreedName, Guid GroomerId, DateTime AppointmentStartAtUtc, DateTime AppointmentEndAtUtc, string Status, DateTime CheckedInAtUtc, DateTime? StartedAtUtc, DateTime? CompletedAtUtc, DateTime? ClosedAtUtc, int ItemCount, decimal AppointmentTotalAmount, decimal AdjustmentTotalAmount, decimal FinalTotalAmount);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
