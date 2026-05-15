using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Staff.Api.Admin;

namespace Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

public sealed class CreateGroomerEndpoint : Endpoint<CreateGroomerRequest, CreateGroomerResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(CreateGroomerRequest req, CancellationToken ct)
    {
        var command = new CreateGroomerUseCaseCommand(req.DisplayName, req.UserId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(GroomerResponseMapper.ToCreateGroomerResponse(result.Value), StatusCodes.Status201Created, ct);
    }
}
