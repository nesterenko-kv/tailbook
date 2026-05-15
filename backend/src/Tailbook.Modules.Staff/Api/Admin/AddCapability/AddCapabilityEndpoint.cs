using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Staff.Api.Admin;
using Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

namespace Tailbook.Modules.Staff.Api.Admin.AddCapability;

public sealed class AddCapabilityEndpoint : Endpoint<AddCapabilityRequest, GroomerCapabilityResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{groomerId:guid}/capabilities");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(AddCapabilityRequest req, CancellationToken ct)
    {
        var result = await new AddGroomerCapabilityUseCaseCommand(
            req.GroomerId,
            req.AnimalTypeId,
            req.BreedId,
            req.BreedGroupId,
            req.CoatTypeId,
            req.SizeCategoryId,
            req.OfferId,
            req.CapabilityMode,
            req.ReservedDurationModifierMinutes,
            req.Notes)
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(GroomerResponseMapper.ToGroomerCapabilityResponse(result.Value), StatusCodes.Status201Created, ct);
    }
}
