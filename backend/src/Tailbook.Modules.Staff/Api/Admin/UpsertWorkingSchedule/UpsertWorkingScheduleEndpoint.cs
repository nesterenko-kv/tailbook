using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

namespace Tailbook.Modules.Staff.Api.Admin.UpsertWorkingSchedule;

public sealed class UpsertWorkingScheduleEndpoint : Endpoint<UpsertWorkingScheduleRequest, WorkingScheduleResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{groomerId:guid}/working-schedules");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(UpsertWorkingScheduleRequest req, CancellationToken ct)
    {
        var command = new UpsertGroomerWorkingScheduleUseCaseCommand(req.GroomerId, req.Weekday, req.StartLocalTime, req.EndLocalTime);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(GroomerResponseMapper.ToWorkingScheduleResponse(result.Value), StatusCodes.Status201Created, ct);
    }
}
