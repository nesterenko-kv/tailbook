using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Staff.Api.Admin.CheckAvailability;

public sealed class CheckAvailabilityEndpoint(IStaffReadService staffReadService)
    : Endpoint<CheckAvailabilityRequest, CheckAvailabilityResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{GroomerId:guid}/availability/check");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.read");
    }

    public override async Task HandleAsync(CheckAvailabilityRequest req, CancellationToken ct)
    {
        var result = await staffReadService.CheckAvailabilityAsync(
            new CheckGroomerAvailabilityCommand(req.GroomerId, req.PetId, req.StartAtUtc, req.ReservedMinutes, req.OfferIds),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(new CheckAvailabilityResponse
        {
            IsAvailable = result.Value.IsAvailable,
            EndAtUtc = result.Value.EndAtUtc,
            CheckedReservedMinutes = result.Value.CheckedReservedMinutes,
            Reasons = result.Value.Reasons.ToArray()
        }, cancellation: ct);
    }
}

public sealed class CheckAvailabilityRequest
{
    public Guid GroomerId { get; set; }
    public Guid PetId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public int ReservedMinutes { get; set; }
    public Guid[] OfferIds { get; set; } = [];
}

public sealed class CheckAvailabilityRequestValidator : Validator<CheckAvailabilityRequest>
{
    public CheckAvailabilityRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ReservedMinutes).GreaterThan(0).LessThanOrEqualTo(1440);
        RuleFor(x => x.OfferIds).NotEmpty();
    }
}

public sealed class CheckAvailabilityResponse
{
    public bool IsAvailable { get; set; }
    public DateTime EndAtUtc { get; set; }
    public int CheckedReservedMinutes { get; set; }
    public string[] Reasons { get; set; } = [];
}
