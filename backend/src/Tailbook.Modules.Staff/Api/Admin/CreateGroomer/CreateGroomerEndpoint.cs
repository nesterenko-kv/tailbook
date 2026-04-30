using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

public sealed class CreateGroomerEndpoint(StaffQueries staffQueries)
    : Endpoint<CreateGroomerRequest, CreateGroomerResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(CreateGroomerRequest req, CancellationToken ct)
    {
        var result = await staffQueries.CreateGroomerAsync(req.DisplayName, req.UserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(Map(result.Value), StatusCodes.Status201Created, ct);
    }

    private static CreateGroomerResponse Map(GroomerDetailView x)
        => new()
        {
            Id = x.Id,
            UserId = x.UserId,
            DisplayName = x.DisplayName,
            Active = x.Active,
            Capabilities = x.Capabilities.Select(c => new GroomerCapabilityResponse
            {
                Id = c.Id,
                GroomerId = c.GroomerId,
                AnimalTypeId = c.AnimalTypeId,
                BreedId = c.BreedId,
                BreedGroupId = c.BreedGroupId,
                CoatTypeId = c.CoatTypeId,
                SizeCategoryId = c.SizeCategoryId,
                OfferId = c.OfferId,
                CapabilityMode = c.CapabilityMode,
                ReservedDurationModifierMinutes = c.ReservedDurationModifierMinutes,
                Notes = c.Notes,
                CreatedAtUtc = c.CreatedAtUtc
            }).ToArray(),
            WorkingSchedules = x.WorkingSchedules.Select(s => new WorkingScheduleResponse
            {
                Id = s.Id,
                GroomerId = s.GroomerId,
                Weekday = s.Weekday,
                StartLocalTime = s.StartLocalTime,
                EndLocalTime = s.EndLocalTime,
                CreatedAtUtc = s.CreatedAtUtc,
                UpdatedAtUtc = s.UpdatedAtUtc
            }).ToArray(),
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        };
}

public sealed class CreateGroomerRequest
{
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class CreateGroomerRequestValidator : Validator<CreateGroomerRequest>
{
    public CreateGroomerRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateGroomerResponse
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public GroomerCapabilityResponse[] Capabilities { get; set; } = [];
    public WorkingScheduleResponse[] WorkingSchedules { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class GroomerCapabilityResponse
{
    public Guid Id { get; set; }
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
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class WorkingScheduleResponse
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public int Weekday { get; set; }
    public string StartLocalTime { get; set; } = string.Empty;
    public string EndLocalTime { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
