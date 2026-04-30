using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Booking.Api.Groomer.ListMyAppointments;

public sealed class ListMyAppointmentsEndpoint(
    GroomerBookingQueries groomerBookingQueries)
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
        try
        {
            var result = await groomerBookingQueries.ListAssignedAppointmentsAsync(req.UserId, req.FromUtc, req.ToUtc, req.Page, req.PageSize, ct);
            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
