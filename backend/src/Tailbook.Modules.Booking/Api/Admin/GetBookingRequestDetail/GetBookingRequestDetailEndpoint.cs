using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.GetBookingRequestDetail;

public sealed class GetBookingRequestDetailEndpoint(
    IBookingManagementReadService bookingReadService,
    IEntityScopeService entityScopeService)
    : Endpoint<GetBookingRequestDetailRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/booking-requests/{bookingRequestId:guid}");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.read");
    }

    public override async Task HandleAsync(GetBookingRequestDetailRequest req, CancellationToken ct)
    {
        var result = await bookingReadService.GetBookingRequestAsync(req.BookingRequestId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.BookingRequest, req.BookingRequestId.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result, ct);
    }
}