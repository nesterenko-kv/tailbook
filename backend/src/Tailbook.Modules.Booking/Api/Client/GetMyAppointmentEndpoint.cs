using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class GetMyAppointmentEndpoint(
    IClientPortalActorService actorService,
    ClientPortalBookingQueries queries)
    : Endpoint<GetMyAppointmentRequest, ClientAppointmentDetailView>
{
    public override void Configure()
    {
        Get("/api/client/appointments/{appointmentId:guid}");
        Description(x => x.WithTags("Client Portal Booking"));
        Permissions("client.appointments.read");
    }

    public override async Task HandleAsync(GetMyAppointmentRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.GetMyAppointmentAsync(actor.ClientId, req.AppointmentId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}