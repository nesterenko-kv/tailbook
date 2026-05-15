using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Notifications.Api.Admin.RequeueNotificationJob;

public sealed class RequeueNotificationJobRequestValidator : Validator<RequeueNotificationJobRequest>
{
    public RequeueNotificationJobRequestValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
    }
}