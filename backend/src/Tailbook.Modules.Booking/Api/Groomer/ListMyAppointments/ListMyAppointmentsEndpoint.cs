using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Groomer.ListMyAppointments;

public sealed class ListMyAppointmentsEndpoint(
    IGroomerBookingReadService groomerBookingReadService)
    : Endpoint<ListMyAppointmentsRequest, PagedResult<GroomerAppointmentListItemView>>
{
    public override void Configure()
    {
        Get("/api/groomer/me/appointments");
        Description(x => x.WithTags("Groomer Booking"));
        PermissionsAll("app.groomer.access", "groomer.appointments.read");
    }

    public override async Task HandleAsync(ListMyAppointmentsRequest req, CancellationToken ct)
    {
        var result = await groomerBookingReadService.ListAssignedAppointmentsAsync(req.UserId, req.From, req.To, req.Page, req.PageSize, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}
