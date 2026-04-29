using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class CreateMyBookingRequestRequestValidator : Validator<CreateMyBookingRequestRequest>
{
    public CreateMyBookingRequestRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.OfferId).NotEmpty();
            item.RuleFor(x => x.ItemType).MaximumLength(32);
            item.RuleFor(x => x.RequestedNotes).MaximumLength(1000);
        });
        RuleForEach(x => x.PreferredTimes).ChildRules(time =>
        {
            time.RuleFor(x => x.StartAtUtc).NotEmpty();
            time.RuleFor(x => x.EndAtUtc).NotEmpty().GreaterThan(x => x.StartAtUtc);
            time.RuleFor(x => x.Label).MaximumLength(200);
        });
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
