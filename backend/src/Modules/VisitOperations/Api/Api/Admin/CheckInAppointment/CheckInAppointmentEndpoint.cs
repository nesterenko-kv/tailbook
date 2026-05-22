using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CheckInAppointment;

public sealed class CheckInAppointmentEndpoint(
    IEntityScopeService entityScopeService)
    : Endpoint<CheckInAppointmentRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/{appointmentId:guid}/check-in");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.write");
    }

    public override async Task HandleAsync(CheckInAppointmentRequest req, CancellationToken ct)
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

        var command = new CheckInAppointmentUseCaseCommand(req.AppointmentId, req.ActorUserId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}