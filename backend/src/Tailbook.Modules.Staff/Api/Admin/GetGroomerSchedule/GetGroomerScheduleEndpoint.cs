using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Staff.Application;

namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerSchedule;

public sealed class GetGroomerScheduleEndpoint(StaffQueries staffQueries)
    : Endpoint<GetGroomerScheduleRequest, GetGroomerScheduleResponse>
{
    public override void Configure()
    {
        Get("/api/admin/groomers/{GroomerId:guid}/schedule");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.read");
    }

    public override async Task HandleAsync(GetGroomerScheduleRequest req, CancellationToken ct)
    {
        try
        {
            var schedule = await staffQueries.GetScheduleAsync(req.GroomerId, req.FromUtc, req.ToUtc, ct);
            if (schedule is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(new GetGroomerScheduleResponse
            {
                GroomerId = schedule.GroomerId,
                GroomerDisplayName = schedule.GroomerDisplayName,
                FromUtc = schedule.FromUtc,
                ToUtc = schedule.ToUtc,
                WorkingSchedules = schedule.WorkingSchedules.Select(x => new WorkingScheduleItemResponse
                {
                    Id = x.Id,
                    GroomerId = x.GroomerId,
                    Weekday = x.Weekday,
                    StartLocalTime = x.StartLocalTime,
                    EndLocalTime = x.EndLocalTime,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc
                }).ToArray(),
                TimeBlocks = schedule.TimeBlocks.Select(x => new TimeBlockItemResponse
                {
                    Id = x.Id,
                    GroomerId = x.GroomerId,
                    StartAtUtc = x.StartAtUtc,
                    EndAtUtc = x.EndAtUtc,
                    ReasonCode = x.ReasonCode,
                    Notes = x.Notes,
                    CreatedAtUtc = x.CreatedAtUtc
                }).ToArray(),
                AvailabilityWindows = schedule.AvailabilityWindows.Select(x => new AvailabilityWindowItemResponse
                {
                    StartAtUtc = x.StartAtUtc,
                    EndAtUtc = x.EndAtUtc
                }).ToArray()
            }, cancellation: ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class GetGroomerScheduleRequest
{
    public Guid GroomerId { get; set; }
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
}

public sealed class GetGroomerScheduleRequestValidator : Validator<GetGroomerScheduleRequest>
{
    public GetGroomerScheduleRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.ToUtc).GreaterThan(x => x.FromUtc);
    }
}

public sealed class GetGroomerScheduleResponse
{
    public Guid GroomerId { get; set; }
    public string GroomerDisplayName { get; set; } = string.Empty;
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public WorkingScheduleItemResponse[] WorkingSchedules { get; set; } = [];
    public TimeBlockItemResponse[] TimeBlocks { get; set; } = [];
    public AvailabilityWindowItemResponse[] AvailabilityWindows { get; set; } = [];
}

public sealed class WorkingScheduleItemResponse
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public int Weekday { get; set; }
    public string StartLocalTime { get; set; } = string.Empty;
    public string EndLocalTime { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class TimeBlockItemResponse
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public DateTimeOffset StartAtUtc { get; set; }
    public DateTimeOffset EndAtUtc { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AvailabilityWindowItemResponse
{
    public DateTimeOffset StartAtUtc { get; set; }
    public DateTimeOffset EndAtUtc { get; set; }
}
