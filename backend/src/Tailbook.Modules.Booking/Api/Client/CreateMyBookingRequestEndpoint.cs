using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Application;

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
        Permissions("client.booking.write");
    }

    public override async Task HandleAsync(CreateMyBookingRequestRequest req, CancellationToken ct)
    {
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
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

            await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}