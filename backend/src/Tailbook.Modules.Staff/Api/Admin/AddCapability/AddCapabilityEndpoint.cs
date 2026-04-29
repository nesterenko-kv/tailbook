using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Staff.Application;
using Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

namespace Tailbook.Modules.Staff.Api.Admin.AddCapability;

public sealed class AddCapabilityEndpoint(StaffQueries staffQueries)
    : Endpoint<AddCapabilityRequest, GroomerCapabilityResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{GroomerId:guid}/capabilities");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(AddCapabilityRequest req, CancellationToken ct)
    {
        var result = await staffQueries.AddCapabilityAsync(
            new AddGroomerCapabilityCommand(req.GroomerId, req.AnimalTypeId, req.BreedId, req.BreedGroupId, req.CoatTypeId, req.SizeCategoryId, req.OfferId, req.CapabilityMode, req.ReservedDurationModifierMinutes, req.Notes),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var capability = result.Value;
        await Send.ResponseAsync(new GroomerCapabilityResponse
        {
            Id = capability.Id,
            GroomerId = capability.GroomerId,
            AnimalTypeId = capability.AnimalTypeId,
            BreedId = capability.BreedId,
            BreedGroupId = capability.BreedGroupId,
            CoatTypeId = capability.CoatTypeId,
            SizeCategoryId = capability.SizeCategoryId,
            OfferId = capability.OfferId,
            CapabilityMode = capability.CapabilityMode,
            ReservedDurationModifierMinutes = capability.ReservedDurationModifierMinutes,
            Notes = capability.Notes,
            CreatedAtUtc = capability.CreatedAtUtc
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class AddCapabilityRequest
{
    public Guid GroomerId { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
    public Guid? OfferId { get; set; }
    public string CapabilityMode { get; set; } = string.Empty;
    public int ReservedDurationModifierMinutes { get; set; }
    public string? Notes { get; set; }
}

public sealed class AddCapabilityRequestValidator : Validator<AddCapabilityRequest>
{
    public AddCapabilityRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.CapabilityMode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.ReservedDurationModifierMinutes).InclusiveBetween(-240, 240);
    }
}
