using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.CancelAppointment;

public sealed class CancelAppointmentRequestValidator : Validator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.ExpectedVersionNo).GreaterThan(0);
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}