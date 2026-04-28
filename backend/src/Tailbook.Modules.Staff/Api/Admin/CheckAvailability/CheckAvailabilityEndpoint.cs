using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Staff.Application;

namespace Tailbook.Modules.Staff.Api.Admin.CheckAvailability;

public sealed class CheckAvailabilityEndpoint(ICurrentUser currentUser, IStaffAccessPolicy accessPolicy, StaffQueries staffQueries)
    : Endpoint<CheckAvailabilityRequest, CheckAvailabilityResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{GroomerId:guid}/availability/check");
        Description(x => x.WithTags("Admin Staff"));
    }

    public override async Task HandleAsync(CheckAvailabilityRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanReadStaff(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await staffQueries.CheckAvailabilityAsync(
                new CheckGroomerAvailabilityCommand(req.GroomerId, req.PetId, req.StartAtUtc, req.ReservedMinutes, req.OfferIds),
                ct);

            await Send.ResponseAsync(new CheckAvailabilityResponse
            {
                IsAvailable = result.IsAvailable,
                EndAtUtc = result.EndAtUtc,
                CheckedReservedMinutes = result.CheckedReservedMinutes,
                Reasons = result.Reasons.ToArray()
            }, cancellation: ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
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
