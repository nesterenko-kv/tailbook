using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Admin.ListBookingRequests;

public sealed class ListBookingRequestsEndpoint(
    IBookingManagementReadService bookingReadService,
    IScopeAuthorizationService scopeAuthorizationService)
    : Endpoint<ListBookingRequestsRequest, PagedResult<BookingRequestListItemView>>
{
    public override void Configure()
    {
        Get("/api/admin/booking-requests");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.read");
    }

    public override async Task HandleAsync(ListBookingRequestsRequest req, CancellationToken ct)
    {
        var result = await bookingReadService.ListBookingRequestsAsync(req.Search, req.Status, req.Page, req.PageSize, ct);

        var actorUserId = User.FindFirst(TailbookClaimTypes.UserId)?.Value;
        IReadOnlyCollection<BookingRequestListItemView> filteredItems = result.Items;
        var totalCount = result.TotalCount;

        if (Guid.TryParse(actorUserId, out var userId))
        {
            var hasGlobal = await scopeAuthorizationService.HasGlobalScopeAsync(userId, ct);
            if (!hasGlobal)
            {
                filteredItems = await ScopeFilter.ApplyAsync(
                    result.Items,
                    userId,
                    EntityScopeResourceTypes.BookingRequest,
                    item => item.Id.ToString("D"),
                    scopeAuthorizationService,
                    ct);
                totalCount = filteredItems.Count;
            }
        }

        var pagedResult = new PagedResult<BookingRequestListItemView>(filteredItems, result.Page, result.PageSize, totalCount);
        await Send.ResponseAsync(pagedResult, cancellation: ct);
    }
}
