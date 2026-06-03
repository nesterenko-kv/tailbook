using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Booking.Application.Booking.Commands;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class RescheduleMyAppointmentEndpoint(
    IClientPortalActorService actorService,
    IClientPortalBookingReadService bookingReadService
)
    : Endpoint<RescheduleMyAppointmentRequest, ClientAppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/client/appointments/{appointmentId:guid}/reschedule");
        Description(x => x.WithTags("Client Portal Booking"));
        PermissionsAll("client.booking.write");
    }

    public override async Task HandleAsync(RescheduleMyAppointmentRequest req, CancellationToken ct)
    {
        var actorResult = await actorService.GetActorAsync(req.UserId, ct);
        if (actorResult.IsError)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var actor = actorResult.Value;

        var verified = await bookingReadService.GetMyAppointmentAsync(actor.ClientId, req.AppointmentId, ct);
        if (verified is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await new RescheduleAppointmentUseCaseCommand(
            req.AppointmentId,
            req.GroomerId,
            req.StartAt,
            req.ExpectedVersionNo,
            actor.UserId
        ).ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var updated = await bookingReadService.GetMyAppointmentAsync(actor.ClientId, req.AppointmentId, ct);
        await Send.OkAsync(updated!, ct);
    }
}
