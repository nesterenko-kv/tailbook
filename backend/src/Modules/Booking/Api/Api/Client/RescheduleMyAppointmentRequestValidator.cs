using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class RescheduleMyAppointmentRequestValidator : Validator<RescheduleMyAppointmentRequest>
{
    public RescheduleMyAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.StartAt).NotEmpty();
        RuleFor(x => x.ExpectedVersionNo).GreaterThan(0);
    }
}
