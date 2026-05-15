using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.CreateBookingRequest;

public sealed class CreateBookingRequestEndpoint : Endpoint<CreateBookingRequestRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(CreateBookingRequestRequest req, CancellationToken ct)
    {
        var result = await new CreateBookingRequestUseCaseCommand(
            req.ClientId,
            req.PetId,
            req.RequestedByContactId,
            req.Channel,
            req.Notes,
            req.PreferredTimes.Select(x => new PreferredTimeWindowInput(x.StartAt, x.EndAt, x.Label)).ToArray(),
            req.Items.Select(x => new CreateBookingRequestItemInput(x.OfferId, x.ItemType, x.RequestedNotes)).ToArray(),
            req.ActorUserId)
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}
