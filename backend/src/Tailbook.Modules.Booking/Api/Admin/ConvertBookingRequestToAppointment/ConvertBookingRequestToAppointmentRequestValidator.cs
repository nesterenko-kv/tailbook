using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.ConvertBookingRequestToAppointment;

public sealed class ConvertBookingRequestToAppointmentRequestValidator : Validator<ConvertBookingRequestToAppointmentRequest>
{
    public ConvertBookingRequestToAppointmentRequestValidator()
    {
        RuleFor(x => x.BookingRequestId).NotEmpty();
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.StartAt).NotEmpty();
    }
}