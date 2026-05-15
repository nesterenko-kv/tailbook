using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerSchedule;

public sealed class GetGroomerScheduleEndpoint(IStaffReadService staffReadService)
    : Endpoint<GetGroomerScheduleRequest, GetGroomerScheduleResponse>
{
    public override void Configure()
    {
        Get("/api/admin/groomers/{groomerId:guid}/schedule");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.read");
    }

    public override async Task HandleAsync(GetGroomerScheduleRequest req, CancellationToken ct)
    {
        var result = await staffReadService.GetScheduleAsync(req.GroomerId, req.From, req.To, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var schedule = result.Value;
        await Send.ResponseAsync(new GetGroomerScheduleResponse
        {
            GroomerId = schedule.GroomerId,
            GroomerDisplayName = schedule.GroomerDisplayName,
            From = schedule.From,
            To = schedule.To,
            WorkingSchedules = schedule.WorkingSchedules.Select(x => new WorkingScheduleItemResponse
            {
                Id = x.Id,
                GroomerId = x.GroomerId,
                Weekday = x.Weekday,
                StartLocalTime = x.StartLocalTime,
                EndLocalTime = x.EndLocalTime,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToArray(),
            TimeBlocks = schedule.TimeBlocks.Select(x => new TimeBlockItemResponse
            {
                Id = x.Id,
                GroomerId = x.GroomerId,
                StartAt = x.StartAt,
                EndAt = x.EndAt,
                ReasonCode = x.ReasonCode,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt
            }).ToArray(),
            AvailabilityWindows = schedule.AvailabilityWindows.Select(x => new AvailabilityWindowItemResponse
            {
                StartAt = x.StartAt,
                EndAt = x.EndAt
            }).ToArray()
        }, cancellation: ct);
    }
}