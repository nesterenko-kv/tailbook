using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Notifications.Api.Admin.ListNotificationJobs;

public sealed class ListNotificationJobsRequestValidator : Validator<ListNotificationJobsRequest>
{
    public ListNotificationJobsRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.CreatedFrom.HasValue || !x.CreatedTo.HasValue || x.CreatedTo.Value >= x.CreatedFrom.Value)
            .WithMessage("CreatedTo must be greater than or equal to CreatedFrom.");
    }
}