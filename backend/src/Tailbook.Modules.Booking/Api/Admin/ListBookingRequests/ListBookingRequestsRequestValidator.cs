using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.ListBookingRequests;

public sealed class ListBookingRequestsRequestValidator : Validator<ListBookingRequestsRequest>
{
    public ListBookingRequestsRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(128);
        RuleFor(x => x.Status).MaximumLength(32);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
