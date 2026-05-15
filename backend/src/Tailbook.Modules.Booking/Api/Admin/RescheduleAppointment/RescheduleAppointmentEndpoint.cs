using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.RescheduleAppointment;

public sealed class RescheduleAppointmentEndpoint(
    IEntityScopeService entityScopeService)
    : Endpoint<RescheduleAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/{appointmentId:guid}/reschedule");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(RescheduleAppointmentRequest req, CancellationToken ct)
    {
        var scopeResult = await entityScopeService.VerifyAccessAsync(
            EntityScopeResourceTypes.Appointment,
            req.AppointmentId.ToString("D"),
            req.ActorUserId,
            ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        var result = await new RescheduleAppointmentUseCaseCommand(
            req.AppointmentId,
            req.GroomerId,
            req.StartAt,
            req.ExpectedVersionNo,
            req.ActorUserId)
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, cancellation: ct);
    }
}