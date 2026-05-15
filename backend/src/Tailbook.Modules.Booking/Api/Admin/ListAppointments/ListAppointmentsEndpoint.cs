using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Admin.ListAppointments;

public sealed class ListAppointmentsEndpoint(
    IBookingManagementReadService bookingReadService,
    IScopeAuthorizationService scopeAuthorizationService)
    : Endpoint<ListAppointmentsRequest, PagedResult<AppointmentListItemView>>
{
    public override void Configure()
    {
        Get("/api/admin/appointments");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.read");
    }

    public override async Task HandleAsync(ListAppointmentsRequest req, CancellationToken ct)
    {
        var result = await bookingReadService.ListAppointmentsAsync(req.Search, req.From, req.To, req.GroomerId, req.Page, req.PageSize, ct);

        var actorUserId = User.FindFirst(TailbookClaimTypes.UserId)?.Value;
        IReadOnlyCollection<AppointmentListItemView> filteredItems = result.Items;
        var totalCount = result.TotalCount;

        if (Guid.TryParse(actorUserId, out var userId))
        {
            var hasGlobal = await scopeAuthorizationService.HasGlobalScopeAsync(userId, ct);
            if (!hasGlobal)
            {
                filteredItems = await ScopeFilter.ApplyAsync(
                    result.Items,
                    userId,
                    EntityScopeResourceTypes.Appointment,
                    item => item.Id.ToString("D"),
                    scopeAuthorizationService,
                    ct);
                totalCount = filteredItems.Count;
            }
        }

        var pagedResult = new PagedResult<AppointmentListItemView>(filteredItems, result.Page, result.PageSize, totalCount);
        await Send.ResponseAsync(pagedResult, cancellation: ct);
    }
}
