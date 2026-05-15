using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicRequesterPayloadValidator : AbstractValidator<PublicRequesterPayload>
{
    public PublicRequesterPayloadValidator()
    {
        RuleFor(x => x.DisplayName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.InstagramHandle).MaximumLength(120);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.PreferredContactMethodCode).MaximumLength(32);
    }
}