using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Groomer.ListMyAppointments;

public sealed class ListMyAppointmentsEndpoint(
    ICurrentUser currentUser,
    IGroomerBookingAccessPolicy accessPolicy,
    GroomerBookingQueries groomerBookingQueries)
    : Endpoint<ListMyAppointmentsRequest, PagedResult<GroomerAppointmentListItemView>>
{
    public override void Configure()
    {
        Get("/api/groomer/me/appointments");
        Description(x => x.WithTags("Groomer Booking"));
    }

    public override async Task HandleAsync(ListMyAppointmentsRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadAssignedAppointments(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await groomerBookingQueries.ListAssignedAppointmentsAsync(currentUserId, req.FromUtc, req.ToUtc, req.Page, req.PageSize, ct);
            await Send.OkAsync(result, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}

public sealed class ListMyAppointmentsRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
