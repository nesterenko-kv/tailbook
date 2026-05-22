using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Staff.Api.Admin.AddTimeBlock;

public sealed class AddTimeBlockEndpoint : Endpoint<AddTimeBlockRequest, AddTimeBlockResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{groomerId:guid}/time-blocks");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(AddTimeBlockRequest req, CancellationToken ct)
    {
        var command = new AddGroomerTimeBlockUseCaseCommand(req.GroomerId, req.StartAt, req.EndAt, req.ReasonCode, req.Notes);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var block = result.Value;
        await Send.ResponseAsync(new AddTimeBlockResponse
        {
            Id = block.Id,
            GroomerId = block.GroomerId,
            StartAt = block.StartAt,
            EndAt = block.EndAt,
            ReasonCode = block.ReasonCode,
            Notes = block.Notes,
            CreatedAt = block.CreatedAt
        }, StatusCodes.Status201Created, ct);
    }
}
