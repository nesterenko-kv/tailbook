using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.ConvertBookingRequestToAppointment;

public sealed class ConvertBookingRequestToAppointmentEndpoint(
    IEntityScopeService entityScopeService)
    : Endpoint<ConvertBookingRequestToAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests/{bookingRequestId:guid}/convert");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(ConvertBookingRequestToAppointmentRequest req, CancellationToken ct)
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

        var command = new ConvertBookingRequestToAppointmentUseCaseCommand(
            req.BookingRequestId,
            req.GroomerId,
            req.StartAt,
            req.ActorUserId
        );

        var result = await command.ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}