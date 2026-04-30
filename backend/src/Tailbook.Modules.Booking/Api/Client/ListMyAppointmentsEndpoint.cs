using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ListMyAppointmentsEndpoint(
    IClientPortalActorService actorService,
    ClientPortalBookingQueries queries)
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
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.ListMyAppointmentsAsync(actor.ClientId, req.FromUtc, ct);
        await Send.OkAsync(result, ct);
    }
}
