using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Admin.CreateBookingRequest;

public sealed class CreateBookingRequestRequestValidator : Validator<CreateBookingRequestRequest>
{
    public CreateBookingRequestRequestValidator()
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
            time.RuleFor(x => x.StartAt).NotEmpty();
            time.RuleFor(x => x.EndAt).NotEmpty().GreaterThan(x => x.StartAt);
            time.RuleFor(x => x.Label).MaximumLength(200);
        });
        RuleFor(x => x.Channel).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}