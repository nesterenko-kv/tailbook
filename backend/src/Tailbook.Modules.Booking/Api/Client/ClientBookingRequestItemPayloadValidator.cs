using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ClientBookingRequestItemPayloadValidator : AbstractValidator<ClientBookingRequestItemPayload>
{
    public ClientBookingRequestItemPayloadValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ItemType).MaximumLength(32);
        RuleFor(x => x.RequestedNotes).MaximumLength(1000);
    }
}