using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class CreateMyBookingRequestRequestValidator : Validator<CreateMyBookingRequestRequest>
{
    public CreateMyBookingRequestRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new ClientBookingRequestItemPayloadValidator());
        RuleForEach(x => x.PreferredTimes).SetValidator(new ClientPreferredTimeWindowPayloadValidator());
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}