using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.GetAppointmentDetail;

public sealed class GetAppointmentDetailEndpoint(
    IBookingManagementReadService bookingReadService,
    IEntityScopeService entityScopeService)
    : Endpoint<GetAppointmentDetailRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Get("/api/admin/appointments/{appointmentId:guid}");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.read");
    }

    public override async Task HandleAsync(GetAppointmentDetailRequest req, CancellationToken ct)
    {
        var result = await bookingReadService.GetAppointmentAsync(req.AppointmentId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.Appointment, req.AppointmentId.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result, cancellation: ct);
    }
}