using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Booking.Api.Contracts.Admin.BulkCancelAppointments;

namespace Tailbook.Modules.Booking.Api.Admin.BulkCancelAppointments;

public sealed class BulkCancelAppointmentsEndpoint : Endpoint<BulkCancelAppointmentsRequest, BulkCancelAppointmentsResponse>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/bulk/cancel");
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(BulkCancelAppointmentsRequest req, CancellationToken ct)
    {
        var command = new BulkCancelAppointmentsUseCaseCommand(
            req.AppointmentIds,
            req.ReasonCode,
            req.Notes,
            req.ActorUserId
        );

        var result = await command.ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, cancellation: ct);
    }
}
