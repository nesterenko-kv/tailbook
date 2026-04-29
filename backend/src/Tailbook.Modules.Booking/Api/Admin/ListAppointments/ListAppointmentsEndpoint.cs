using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.ListAppointments;

public sealed class ListAppointmentsEndpoint(BookingManagementQueries bookingQueries)
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
        var result = await bookingQueries.ListAppointmentsAsync(req.FromUtc, req.ToUtc, req.GroomerId, req.Page, req.PageSize, ct);
        await Send.ResponseAsync(result, cancellation: ct);
    }
}

public sealed class ListAppointmentsRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public Guid? GroomerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ListAppointmentsRequestValidator : Validator<ListAppointmentsRequest>
{
    public ListAppointmentsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x).Must(x => !x.FromUtc.HasValue || !x.ToUtc.HasValue || x.ToUtc.Value > x.FromUtc.Value)
            .WithMessage("toUtc must be later than fromUtc.");
    }
}
