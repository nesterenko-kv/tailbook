using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerSchedule;

public sealed class GetGroomerScheduleRequestValidator : Validator<GetGroomerScheduleRequest>
{
    public GetGroomerScheduleRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.To).GreaterThan(x => x.From);
    }
}