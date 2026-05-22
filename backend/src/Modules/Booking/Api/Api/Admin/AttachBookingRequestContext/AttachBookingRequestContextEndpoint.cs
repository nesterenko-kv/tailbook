using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.AttachBookingRequestContext;

public sealed class AttachBookingRequestContextEndpoint(
    IEntityScopeService entityScopeService)
    : Endpoint<AttachBookingRequestContextRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests/{bookingRequestId:guid}/attach-context");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(AttachBookingRequestContextRequest req, CancellationToken ct)
    {
        var scopeResult = await entityScopeService.VerifyAccessAsync(
            EntityScopeResourceTypes.BookingRequest,
            req.BookingRequestId.ToString("D"),
            req.ActorUserId,
            ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        var result = await new AttachBookingRequestContextUseCaseCommand(
            new AttachBookingRequestContextData(
                req.BookingRequestId,
                req.ClientId,
                req.PetId,
                req.RequestedByContactId),
            req.ActorUserId)
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}