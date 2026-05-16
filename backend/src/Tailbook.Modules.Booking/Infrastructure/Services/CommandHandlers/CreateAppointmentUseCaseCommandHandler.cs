using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class CreateAppointmentUseCaseCommandHandler(
    AppDbContext dbContext,
    IBookingManagementReadService bookingReadService,
    IBookingSnapshotComposer bookingSnapshotComposer,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher,
    TimeProvider timeProvider)
    : ICommandHandler<CreateAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>
{
    public Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(CreateAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        return CreateAppointmentAsync(
            null,
            command.PetId,
            command.GroomerId,
            command.StartAt,
            command.Items.Select(x => new PreviewQuoteItemQuery(x.OfferId, x.ItemType)).ToArray(),
            command.ActorUserId,
            ct);
    }

    public async Task<ErrorOr<AppointmentDetailView>> CreateAppointmentAsync(
        Guid? bookingRequestId,
        Guid petId,
        Guid groomerId,
        DateTimeOffset startAt,
        IReadOnlyCollection<PreviewQuoteItemQuery> items,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var compositionResult = await bookingSnapshotComposer.ComposeAppointmentAsync(
            petId,
            groomerId,
            startAt,
            items,
            actorUserId,
            cancellationToken);
        if (compositionResult.IsError)
        {
            return compositionResult.Errors;
        }

        var composition = compositionResult.Value;
        var appointmentPeriod = BookingTimeInputNormalizer.CreatePeriod(composition.StartAt, composition.EndAt);
        if (appointmentPeriod.IsError)
        {
            return appointmentPeriod.Errors;
        }

        var utcNow = timeProvider.GetUtcNow();
        var appointmentResult = Appointment.Create(
            Guid.NewGuid(),
            bookingRequestId,
            petId,
            groomerId,
            appointmentPeriod.Value,
            composition.Items.Select(x => new AppointmentItemDraft(
                x.OfferType,
                x.OfferId,
                x.OfferVersionId,
                x.OfferCode,
                x.DisplayName,
                1,
                x.PriceSnapshot.Id,
                x.DurationSnapshot.Id)).ToArray(),
            actorUserId,
            utcNow);
        if (appointmentResult.IsError)
        {
            return appointmentResult.Errors;
        }

        var appointment = appointmentResult.Value;
        dbContext.Set<Appointment>().Add(appointment);

        await outboxPublisher.PublishAsync("booking", "AppointmentCreated", new
        {
            appointmentId = appointment.Id,
            bookingRequestId = appointment.BookingRequestId,
            petId = appointment.PetId,
            groomerId = appointment.GroomerId,
            startAt = appointment.StartAt,
            endAt = appointment.EndAt,
            status = appointment.Status,
            versionNo = appointment.VersionNo
        }, cancellationToken);

        var saveResult = await ConcurrencySafeSaver.SaveAsync(dbContext, cancellationToken);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }
        await auditTrailService.RecordAsync(
            "booking",
            "appointment",
            appointment.Id.ToString("D"),
            "CREATE",
            actorUserId,
            null,
            JsonSerializer.Serialize(new { appointment.Status, appointment.VersionNo }),
            cancellationToken);

        return (await bookingReadService.GetAppointmentAsync(appointment.Id, cancellationToken))!;
    }
}
