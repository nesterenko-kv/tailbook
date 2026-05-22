using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Notifications.Api.Admin.AbandonNotificationJob;

public sealed class AbandonNotificationJobRequestValidator : Validator<AbandonNotificationJobRequest>
{
    public AbandonNotificationJobRequestValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
    }
}