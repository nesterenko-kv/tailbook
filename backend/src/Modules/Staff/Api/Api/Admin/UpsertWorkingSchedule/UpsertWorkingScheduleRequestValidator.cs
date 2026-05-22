using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Staff.Api.Admin.UpsertWorkingSchedule;

public sealed class UpsertWorkingScheduleRequestValidator : Validator<UpsertWorkingScheduleRequest>
{
    public UpsertWorkingScheduleRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.Weekday).InclusiveBetween(1, 7);
        RuleFor(x => x.StartLocalTime).NotEmpty().Matches("^\\d{2}:\\d{2}$");
        RuleFor(x => x.EndLocalTime).NotEmpty().Matches("^\\d{2}:\\d{2}$");
    }
}