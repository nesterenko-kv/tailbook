using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class CreateMyBookingRequestEndpoint(
    IClientPortalActorService actorService,
    ClientPortalBookingQueries queries)
    : Endpoint<CreateMyBookingRequestRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/client/booking-requests");
        Description(x => x.WithTags("Client Portal Booking"));
        PermissionsAll("client.booking.write");
    }

    public override async Task HandleAsync(CreateMyBookingRequestRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.CreateMyBookingRequestAsync(
            actor,
            new CreateClientBookingRequestCommand(
                req.PetId,
                req.Notes,
                req.PreferredTimes.Select(x => new PreferredTimeWindowCommand(x.StartAtUtc, x.EndAtUtc, x.Label))
                    .ToArray(),
                req.Items.Select(x =>
                    new CreateClientBookingRequestItemCommand(x.OfferId, x.ItemType, x.RequestedNotes)).ToArray()),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}
