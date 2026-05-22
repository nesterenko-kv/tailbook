using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.ListAppointments;

public sealed class ListAppointmentsRequestValidator : Validator<ListAppointmentsRequest>
{
    public ListAppointmentsRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(128);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.To.Value > x.From.Value)
            .WithMessage("to must be later than from.");
    }
}
