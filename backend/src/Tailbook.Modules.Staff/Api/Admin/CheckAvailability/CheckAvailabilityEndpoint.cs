using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Staff.Api.Admin.CheckAvailability;

public sealed class CheckAvailabilityEndpoint(IStaffReadService staffReadService)
    : Endpoint<CheckAvailabilityRequest, CheckAvailabilityResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{groomerId:guid}/availability/check");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.read");
    }

    public override async Task HandleAsync(CheckAvailabilityRequest req, CancellationToken ct)
    {
        var result = await staffReadService.CheckAvailabilityAsync(
            new CheckGroomerAvailabilityQuery(req.GroomerId, req.PetId, req.StartAt, req.ReservedMinutes, req.OfferIds),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(new CheckAvailabilityResponse
        {
            IsAvailable = result.Value.IsAvailable,
            EndAt = result.Value.EndAt,
            CheckedReservedMinutes = result.Value.CheckedReservedMinutes,
            Reasons = result.Value.Reasons.ToArray()
        }, cancellation: ct);
    }
}