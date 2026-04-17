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
    IAccessAuditService accessAuditService)
{
    public async Task<VisitDetailView?> CheckInAppointmentAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var existingVisit = await dbContext.Set<Visit>().SingleOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        if (existingVisit is not null)
        {
            throw new InvalidOperationException("Appointment has already been checked in.");
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        await appointmentVisitService.MarkCheckedInAsync(appointmentId, actorUserId, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Status = VisitStatusCodes.Open,
            CheckedInAtUtc = utcNow,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<Visit>().Add(visit);
        dbContext.Set<VisitExecutionItem>().AddRange(appointment.Items.Select(x => new VisitExecutionItem
        {
            Id = Guid.NewGuid(),
            VisitId = visit.Id,
            AppointmentItemId = x.AppointmentItemId,
            ItemType = x.ItemType,
            OfferId = x.OfferId,
            OfferVersionId = x.OfferVersionId,
            OfferCodeSnapshot = x.OfferCode,
            OfferDisplayNameSnapshot = x.OfferDisplayName,
            Quantity = x.Quantity,
            PriceAmountSnapshot = x.PriceAmount,
            ServiceMinutesSnapshot = x.ServiceMinutes,
            ReservedMinutesSnapshot = x.ReservedMinutes,
            CreatedAtUtc = utcNow
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visit.Id, actorUserId, cancellationToken);
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

    public async Task<VisitDetailView?> RecordPerformedProcedureAsync(Guid visitId, Guid visitExecutionItemId, Guid procedureId, string? note, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(visitId, cancellationToken);
        EnsureEditable(visit);

        var executionItem = await dbContext.Set<VisitExecutionItem>()
            .SingleOrDefaultAsync(x => x.Id == visitExecutionItemId && x.VisitId == visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit execution item does not exist.");

        var exists = await dbContext.Set<VisitPerformedProcedure>()
            .AnyAsync(x => x.VisitExecutionItemId == visitExecutionItemId && x.ProcedureId == procedureId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Procedure has already been recorded for this execution item.");
        }

        var procedure = await visitCatalogReadService.GetProcedureAsync(procedureId, cancellationToken)
            ?? throw new InvalidOperationException("Procedure does not exist.");

        dbContext.Set<VisitPerformedProcedure>().Add(new VisitPerformedProcedure
        {
            Id = Guid.NewGuid(),
            VisitExecutionItemId = executionItem.Id,
            ProcedureId = procedure.Id,
            ProcedureCodeSnapshot = procedure.Code,
            ProcedureNameSnapshot = procedure.Name,
            Status = ProcedureExecutionStatusCodes.Performed,
            Note = NormalizeOptional(note),
            RecordedByUserId = actorUserId,
            RecordedAtUtc = DateTime.UtcNow
        });

        await EnsureInProgressAsync(visit, actorUserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<VisitDetailView?> RecordSkippedComponentAsync(Guid visitId, Guid visitExecutionItemId, Guid offerVersionComponentId, string omissionReasonCode, string? note, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(visitId, cancellationToken);
        EnsureEditable(visit);

        var executionItem = await dbContext.Set<VisitExecutionItem>()
            .SingleOrDefaultAsync(x => x.Id == visitExecutionItemId && x.VisitId == visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit execution item does not exist.");

        var component = await visitCatalogReadService.GetComponentAsync(offerVersionComponentId, cancellationToken)
            ?? throw new InvalidOperationException("Offer version component does not exist.");

        if (component.OfferVersionId != executionItem.OfferVersionId)
        {
            throw new InvalidOperationException("Selected component does not belong to this visit execution item.");
        }

        if (!string.Equals(component.ComponentRole, "Included", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only included components can be marked as skipped.");
        }

        var exists = await dbContext.Set<VisitSkippedComponent>()
            .AnyAsync(x => x.VisitExecutionItemId == visitExecutionItemId && x.OfferVersionComponentId == offerVersionComponentId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Component has already been marked as skipped for this execution item.");
        }

        dbContext.Set<VisitSkippedComponent>().Add(new VisitSkippedComponent
        {
            Id = Guid.NewGuid(),
            VisitExecutionItemId = executionItem.Id,
            OfferVersionComponentId = component.Id,
            ProcedureId = component.ProcedureId,
            ProcedureCodeSnapshot = component.ProcedureCode,
            ProcedureNameSnapshot = component.ProcedureName,
            OmissionReasonCode = NormalizeRequiredCode(omissionReasonCode, "Omission reason code is required."),
            Note = NormalizeOptional(note),
            RecordedByUserId = actorUserId,
            RecordedAtUtc = DateTime.UtcNow
        });

        await EnsureInProgressAsync(visit, actorUserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<VisitDetailView?> ApplyPriceAdjustmentAsync(Guid visitId, int sign, decimal amount, string reasonCode, string? note, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(visitId, cancellationToken);
        EnsureEditable(visit, allowAwaitingFinalization: true);

        if (sign is not -1 and not 1)
        {
            throw new InvalidOperationException("Adjustment sign must be either -1 or 1.");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("Adjustment amount must be greater than zero.");
        }

        dbContext.Set<VisitPriceAdjustment>().Add(new VisitPriceAdjustment
        {
            Id = Guid.NewGuid(),
            VisitId = visit.Id,
            Sign = sign,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            ReasonCode = NormalizeRequiredCode(reasonCode, "Adjustment reason code is required."),
            Note = NormalizeOptional(note),
            CreatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        visit.UpdatedAtUtc = DateTime.UtcNow;
        visit.UpdatedByUserId = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<VisitDetailView?> CompleteVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(visitId, cancellationToken);
        if (visit.Status is not VisitStatusCodes.Open and not VisitStatusCodes.InProgress)
        {
            throw new InvalidOperationException("Visit is not eligible for completion.");
        }

        visit.Status = VisitStatusCodes.AwaitingFinalization;
        visit.CompletedAtUtc = DateTime.UtcNow;
        visit.UpdatedAtUtc = visit.CompletedAtUtc.Value;
        visit.UpdatedByUserId = actorUserId;

        await appointmentVisitService.MarkCompletedAsync(visit.AppointmentId, actorUserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    public async Task<VisitDetailView?> CloseVisitAsync(Guid visitId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(visitId, cancellationToken);
        if (visit.Status != VisitStatusCodes.AwaitingFinalization)
        {
            throw new InvalidOperationException("Visit is not eligible for closure.");
        }

        visit.Status = VisitStatusCodes.Closed;
        visit.ClosedAtUtc = DateTime.UtcNow;
        visit.UpdatedAtUtc = visit.ClosedAtUtc.Value;
        visit.UpdatedByUserId = actorUserId;

        await appointmentVisitService.MarkClosedAsync(visit.AppointmentId, actorUserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visitId, actorUserId, cancellationToken);
    }

    private async Task EnsureInProgressAsync(Visit visit, Guid? actorUserId, CancellationToken cancellationToken)
    {
        if (visit.Status != VisitStatusCodes.Open)
        {
            return;
        }

        visit.Status = VisitStatusCodes.InProgress;
        visit.StartedAtUtc ??= DateTime.UtcNow;
        visit.UpdatedAtUtc = visit.StartedAtUtc.Value;
        visit.UpdatedByUserId = actorUserId;
        await appointmentVisitService.MarkInProgressAsync(visit.AppointmentId, actorUserId, cancellationToken);
    }

    private async Task<Visit> LoadVisitAsync(Guid visitId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Visit>().SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken)
               ?? throw new InvalidOperationException("Visit does not exist.");
    }

    private static void EnsureEditable(Visit visit, bool allowAwaitingFinalization = false)
    {
        var editableStatuses = allowAwaitingFinalization
            ? new[] { VisitStatusCodes.Open, VisitStatusCodes.InProgress, VisitStatusCodes.AwaitingFinalization }
            : new[] { VisitStatusCodes.Open, VisitStatusCodes.InProgress };

        if (!editableStatuses.Contains(visit.Status, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Visit is not editable in its current status.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeRequiredCode(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            throw new InvalidOperationException(message);
        }

        return normalized.ToUpperInvariant();
    }
}

public sealed record VisitPetView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode);
public sealed record VisitExpectedComponentView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string ComponentRole, int SequenceNo, bool DefaultExpected, bool IsSkipped);
public sealed record VisitPerformedProcedureView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string Status, string? Note, DateTime RecordedAtUtc);
public sealed record VisitSkippedComponentView(Guid Id, Guid OfferVersionComponentId, Guid ProcedureId, string ProcedureCode, string ProcedureName, string OmissionReasonCode, string? Note, DateTime RecordedAtUtc);
public sealed record VisitExecutionItemView(Guid Id, Guid AppointmentItemId, string ItemType, Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferDisplayName, int Quantity, decimal PriceAmount, int ServiceMinutes, int ReservedMinutes, IReadOnlyCollection<VisitExpectedComponentView> ExpectedComponents, IReadOnlyCollection<VisitPerformedProcedureView> PerformedProcedures, IReadOnlyCollection<VisitSkippedComponentView> SkippedComponents);
public sealed record VisitPriceAdjustmentView(Guid Id, int Sign, decimal Amount, string ReasonCode, string? Note, DateTime CreatedAtUtc);
public sealed record VisitDetailView(Guid Id, Guid AppointmentId, Guid? BookingRequestId, VisitPetView Pet, Guid GroomerId, string Status, DateTime CheckedInAtUtc, DateTime? StartedAtUtc, DateTime? CompletedAtUtc, DateTime? ClosedAtUtc, int ServiceMinutes, int ReservedMinutes, decimal AppointmentTotalAmount, decimal AdjustmentTotalAmount, decimal FinalTotalAmount, IReadOnlyCollection<VisitExecutionItemView> Items, IReadOnlyCollection<VisitPriceAdjustmentView> Adjustments, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
