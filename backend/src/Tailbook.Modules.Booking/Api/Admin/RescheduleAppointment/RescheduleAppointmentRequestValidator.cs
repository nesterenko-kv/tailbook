using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.RescheduleAppointment;

public sealed class RescheduleAppointmentRequestValidator : Validator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.StartAt).NotEmpty();
        RuleFor(x => x.ExpectedVersionNo).GreaterThan(0);
    }
}