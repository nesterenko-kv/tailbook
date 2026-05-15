using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.CheckInAppointment;

public sealed class CheckInOwnAppointmentRequestValidator : Validator<CheckInOwnAppointmentRequest>
{
    public CheckInOwnAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}