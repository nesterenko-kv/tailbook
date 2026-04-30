using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Booking.Api.Admin.ListBookingRequests;

public sealed class ListBookingRequestsEndpoint(IBookingManagementQueries bookingQueries)
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
        var result = await bookingQueries.ListBookingRequestsAsync(req.Status, req.Page, req.PageSize, ct);
        await Send.ResponseAsync(result, cancellation: ct);
    }
}

public sealed class ListBookingRequestsRequest
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ListBookingRequestsRequestValidator : Validator<ListBookingRequestsRequest>
{
    public ListBookingRequestsRequestValidator()
    {
        RuleFor(x => x.Status).MaximumLength(32);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
