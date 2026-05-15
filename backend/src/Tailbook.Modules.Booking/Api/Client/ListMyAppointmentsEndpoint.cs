using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ListMyAppointmentsEndpoint(
    IClientPortalActorService actorService,
    IClientPortalBookingReadService bookingReadService)
    : Endpoint<ListMyAppointmentsRequest, IReadOnlyCollection<ClientAppointmentSummaryView>>
{
    public override void Configure()
    {
        Get("/api/client/appointments");
        Description(x => x.WithTags("Client Portal Booking"));
        PermissionsAll(PermissionCodes.ClientAppointmentsRead);
    }

    public override async Task HandleAsync(ListMyAppointmentsRequest req, CancellationToken ct)
    {
        var actorResult = await actorService.GetActorAsync(req.UserId, ct);
        if (actorResult.IsError)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var actor = actorResult.Value;
        var result = await bookingReadService.ListMyAppointmentsAsync(actor.ClientId, req.From, ct);
        await Send.OkAsync(result, ct);
    }
}
