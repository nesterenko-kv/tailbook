using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.AttachBookingRequestContext;

public sealed class AttachBookingRequestContextRequestValidator : Validator<AttachBookingRequestContextRequest>
{
    public AttachBookingRequestContextRequestValidator()
    {
        RuleFor(x => x.BookingRequestId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
    }
}