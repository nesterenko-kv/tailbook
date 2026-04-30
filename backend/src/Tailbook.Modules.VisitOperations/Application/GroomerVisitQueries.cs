using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations.Domain;

namespace Tailbook.Modules.VisitOperations.Application;

public sealed class GroomerVisitQueries(
    AppDbContext dbContext,
    VisitQueries visitQueries,
    IAppointmentVisitService appointmentVisitService,
    IGroomerProfileReadService groomerProfileReadService)
{
    public async Task<ErrorOr<GroomerVisitDetailView>> CheckInAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken)
    {
        var groomer = await GetLinkedActiveGroomerAsync(currentUserId, cancellationToken);
        if (groomer.IsError)
        {
            return groomer.Errors;
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null || appointment.GroomerId != groomer.Value.GroomerId)
        {
            return Error.NotFound("VisitOperations.AppointmentNotFound", "Appointment does not exist.");
        }

        var result = await visitQueries.CheckInAppointmentAsync(appointmentId, currentUserId, cancellationToken);
        return result.IsError ? result.Errors : Map(result.Value);
    }

    public async Task<ErrorOr<GroomerVisitDetailView>> GetVisitByAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken)
    {
        var groomer = await GetLinkedActiveGroomerAsync(currentUserId, cancellationToken);
        if (groomer.IsError)
        {
            return groomer.Errors;
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(appointmentId, cancellationToken);
        if (appointment is null || appointment.GroomerId != groomer.Value.GroomerId)
        {
            return Error.NotFound("VisitOperations.AppointmentNotFound", "Appointment does not exist.");
        }

        var visitId = await dbContext.Set<Visit>()
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!visitId.HasValue)
        {
            return Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.");
        }

        var result = await visitQueries.GetVisitAsync(visitId.Value, currentUserId, cancellationToken, recordAccessAudit: false);
        return result is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : Map(result);
    }

    public async Task<ErrorOr<GroomerVisitDetailView>> GetVisitAsync(Guid currentUserId, Guid visitId, CancellationToken cancellationToken)
    {
        var groomer = await GetLinkedActiveGroomerAsync(currentUserId, cancellationToken);
        if (groomer.IsError)
        {
            return groomer.Errors;
        }

        var result = await visitQueries.GetVisitAsync(visitId, currentUserId, cancellationToken, recordAccessAudit: false);
        if (result is null || result.GroomerId != groomer.Value.GroomerId)
        {
            return Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.");
        }

        return Map(result);
    }

    public async Task<ErrorOr<GroomerVisitDetailView>> RecordPerformedProcedureAsync(Guid currentUserId, Guid visitId, Guid visitExecutionItemId, Guid procedureId, string? note, CancellationToken cancellationToken)
    {
        var existing = await GetVisitAsync(currentUserId, visitId, cancellationToken);
        if (existing.IsError)
        {
            return existing.Errors;
        }

        var result = await visitQueries.RecordPerformedProcedureAsync(visitId, visitExecutionItemId, procedureId, note, currentUserId, cancellationToken);
        return result.IsError ? result.Errors : Map(result.Value);
    }

    public async Task<ErrorOr<GroomerVisitDetailView>> RecordSkippedComponentAsync(Guid currentUserId, Guid visitId, Guid visitExecutionItemId, Guid offerVersionComponentId, string omissionReasonCode, string? note, CancellationToken cancellationToken)
    {
        var existing = await GetVisitAsync(currentUserId, visitId, cancellationToken);
        if (existing.IsError)
        {
            return existing.Errors;
        }

        var result = await visitQueries.RecordSkippedComponentAsync(visitId, visitExecutionItemId, offerVersionComponentId, omissionReasonCode, note, currentUserId, cancellationToken);
        return result.IsError ? result.Errors : Map(result.Value);
    }

    private async Task<ErrorOr<GroomerProfileReadModel>> GetLinkedActiveGroomerAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var groomer = await groomerProfileReadService.GetByUserIdAsync(currentUserId, cancellationToken);
        if (groomer is null || !groomer.Active)
        {
            return Error.Forbidden("VisitOperations.GroomerProfileRequired", "Current user is not linked to an active groomer profile.");
        }

        return groomer;
    }

    private static GroomerVisitDetailView Map(VisitDetailView view)
    {
        return new GroomerVisitDetailView(
            view.Id,
            view.AppointmentId,
            new GroomerVisitPetView(view.Pet.Id, view.Pet.Name, view.Pet.AnimalTypeCode, view.Pet.AnimalTypeName, view.Pet.BreedName, view.Pet.CoatTypeCode, view.Pet.SizeCategoryCode),
            view.Status,
            view.CheckedInAtUtc,
            view.StartedAtUtc,
            view.CompletedAtUtc,
            view.ClosedAtUtc,
            view.ServiceMinutes,
            view.ReservedMinutes,
            view.Items.Select(item => new GroomerVisitExecutionItemView(
                item.Id,
                item.AppointmentItemId,
                item.ItemType,
                item.OfferId,
                item.OfferVersionId,
                item.OfferCode,
                item.OfferDisplayName,
                item.Quantity,
                item.ServiceMinutes,
                item.ReservedMinutes,
                item.ExpectedComponents.Select(component => new GroomerVisitExpectedComponentView(
                    component.Id,
                    component.ProcedureId,
                    component.ProcedureCode,
                    component.ProcedureName,
                    component.ComponentRole,
                    component.SequenceNo,
                    component.DefaultExpected,
                    component.IsSkipped)).ToArray(),
                item.PerformedProcedures.Select(performed => new GroomerVisitPerformedProcedureView(
                    performed.Id,
                    performed.ProcedureId,
                    performed.ProcedureCode,
                    performed.ProcedureName,
                    performed.Status,
                    performed.Note,
                    performed.RecordedAtUtc)).ToArray(),
                item.SkippedComponents.Select(skipped => new GroomerVisitSkippedComponentView(
                    skipped.Id,
                    skipped.OfferVersionComponentId,
                    skipped.ProcedureId,
                    skipped.ProcedureCode,
                    skipped.ProcedureName,
                    skipped.OmissionReasonCode,
                    skipped.Note,
                    skipped.RecordedAtUtc)).ToArray())).ToArray(),
            view.CreatedAtUtc,
            view.UpdatedAtUtc);
    }
}

public sealed record GroomerVisitPetView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode);
public sealed record GroomerVisitExpectedComponentView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string ComponentRole, int SequenceNo, bool DefaultExpected, bool IsSkipped);
public sealed record GroomerVisitPerformedProcedureView(Guid Id, Guid ProcedureId, string ProcedureCode, string ProcedureName, string Status, string? Note, DateTime RecordedAtUtc);
public sealed record GroomerVisitSkippedComponentView(Guid Id, Guid OfferVersionComponentId, Guid ProcedureId, string ProcedureCode, string ProcedureName, string OmissionReasonCode, string? Note, DateTime RecordedAtUtc);
public sealed record GroomerVisitExecutionItemView(Guid Id, Guid AppointmentItemId, string ItemType, Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferDisplayName, int Quantity, int ServiceMinutes, int ReservedMinutes, IReadOnlyCollection<GroomerVisitExpectedComponentView> ExpectedComponents, IReadOnlyCollection<GroomerVisitPerformedProcedureView> PerformedProcedures, IReadOnlyCollection<GroomerVisitSkippedComponentView> SkippedComponents);
public sealed record GroomerVisitDetailView(Guid Id, Guid AppointmentId, GroomerVisitPetView Pet, string Status, DateTime CheckedInAtUtc, DateTime? StartedAtUtc, DateTime? CompletedAtUtc, DateTime? ClosedAtUtc, int ServiceMinutes, int ReservedMinutes, IReadOnlyCollection<GroomerVisitExecutionItemView> Items, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
