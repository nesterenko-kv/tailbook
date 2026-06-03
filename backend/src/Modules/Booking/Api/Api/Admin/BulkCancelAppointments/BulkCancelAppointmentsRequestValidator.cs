using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.BulkCancelAppointments;

public sealed class BulkCancelAppointmentsRequestValidator : Validator<BulkCancelAppointmentsRequest>
{
    public BulkCancelAppointmentsRequestValidator()
    {
        RuleFor(x => x.AppointmentIds).NotEmpty().WithMessage("At least one appointment must be specified.");
        RuleFor(x => x.AppointmentIds).Must(x => x.Length <= 100).WithMessage("Cannot cancel more than 100 appointments at once.");
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
