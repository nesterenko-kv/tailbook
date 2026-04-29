using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Staff.Application;
using Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

namespace Tailbook.Modules.Staff.Api.Admin.UpsertWorkingSchedule;

public sealed class UpsertWorkingScheduleEndpoint(StaffQueries staffQueries)
    : Endpoint<UpsertWorkingScheduleRequest, WorkingScheduleResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{GroomerId:guid}/working-schedules");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(UpsertWorkingScheduleRequest req, CancellationToken ct)
    {
        var result = await staffQueries.UpsertWorkingScheduleAsync(req.GroomerId, req.Weekday, req.StartLocalTime, req.EndLocalTime, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var schedule = result.Value;
        await Send.ResponseAsync(new WorkingScheduleResponse
        {
            Id = schedule.Id,
            GroomerId = schedule.GroomerId,
            Weekday = schedule.Weekday,
            StartLocalTime = schedule.StartLocalTime,
            EndLocalTime = schedule.EndLocalTime,
            CreatedAtUtc = schedule.CreatedAtUtc,
            UpdatedAtUtc = schedule.UpdatedAtUtc
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class UpsertWorkingScheduleRequest
{
    public Guid GroomerId { get; set; }
    public int Weekday { get; set; }
    public string StartLocalTime { get; set; } = string.Empty;
    public string EndLocalTime { get; set; } = string.Empty;
}

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
