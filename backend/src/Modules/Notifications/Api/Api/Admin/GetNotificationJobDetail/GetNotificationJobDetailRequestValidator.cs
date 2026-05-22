using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Notifications.Api.Admin.GetNotificationJobDetail;

public sealed class GetNotificationJobDetailRequestValidator : Validator<GetNotificationJobDetailRequest>
{
    public GetNotificationJobDetailRequestValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
    }
}