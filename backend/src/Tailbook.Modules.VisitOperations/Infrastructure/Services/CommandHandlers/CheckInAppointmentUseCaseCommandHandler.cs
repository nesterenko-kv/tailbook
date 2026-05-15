using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class CheckInAppointmentUseCaseCommandHandler(
    AppDbContext dbContext,
    IVisitReadService visitReadService,
    IAppointmentVisitService appointmentVisitService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher,
    TimeProvider timeProvider)
    : ICommandHandler<CheckInAppointmentUseCaseCommand, ErrorOr<VisitDetailView>>
{
    public async Task<ErrorOr<VisitDetailView>> ExecuteAsync(CheckInAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        var existingVisit = await dbContext.Set<Visit>().SingleOrDefaultAsync(x => x.AppointmentId == command.AppointmentId, ct);
        if (existingVisit is not null)
        {
            return Error.Conflict("VisitOperations.AppointmentAlreadyCheckedIn", "Appointment has already been checked in.");
        }

        var appointment = await appointmentVisitService.GetAppointmentAsync(command.AppointmentId, ct);
        if (appointment is null)
        {
            return Error.NotFound("VisitOperations.AppointmentNotFound", "Appointment does not exist.");
        }

        var markCheckedInResult = await appointmentVisitService.MarkCheckedInAsync(command.AppointmentId, command.ActorUserId, ct);
        if (markCheckedInResult.IsError)
        {
            return markCheckedInResult.Errors;
        }

        var utcNow = timeProvider.GetUtcNow();
        var visitResult = Visit.CheckIn(
            Guid.NewGuid(),
            command.AppointmentId,
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
            command.ActorUserId,
            utcNow);
        if (visitResult.IsError)
        {
            return visitResult.Errors;
        }

        var visit = visitResult.Value;
        dbContext.Set<Visit>().Add(visit);

        await outboxPublisher.PublishAsync("visitops", "VisitCheckedIn", new
        {
            visitId = visit.Id,
            appointmentId = visit.AppointmentId,
            status = visit.Status,
            checkedInAt = visit.CheckedInAt
        }, ct);
        await dbContext.SaveChangesAsync(ct);
        await auditTrailService.RecordAsync(
            "visitops",
            "visit",
            visit.Id.ToString("D"),
            "CHECK_IN",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { visit.Status }),
            ct);

        return await ReadVisitAsync(visit.Id, command.ActorUserId, ct);
    }

    private async Task<ErrorOr<VisitDetailView>> ReadVisitAsync(Guid visitId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var visit = await visitReadService.GetVisitAsync(visitId, actorUserId, cancellationToken);
        return visit is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : visit;
    }
}
