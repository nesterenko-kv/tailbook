using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ClientPreferredTimeWindowPayloadValidator : AbstractValidator<ClientPreferredTimeWindowPayload>
{
    public ClientPreferredTimeWindowPayloadValidator()
    {
        RuleFor(x => x.StartAtUtc).NotEmpty();
        RuleFor(x => x.EndAtUtc).NotEmpty().GreaterThan(x => x.StartAtUtc);
        RuleFor(x => x.Label).MaximumLength(200);
    }
}