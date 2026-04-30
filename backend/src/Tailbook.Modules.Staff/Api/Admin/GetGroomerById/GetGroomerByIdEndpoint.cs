using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerById;

public sealed class GetGroomerByIdEndpoint(IStaffReadService staffReadService)
    : Endpoint<GetGroomerByIdRequest, CreateGroomerResponse>
{
    public override void Configure()
    {
        Get("/api/admin/groomers/{GroomerId:guid}");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.read");
    }

    public override async Task HandleAsync(GetGroomerByIdRequest req, CancellationToken ct)
    {
        var groomer = await staffReadService.GetGroomerAsync(req.GroomerId, ct);
        if (groomer is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(new CreateGroomerResponse
        {
            Id = groomer.Id,
            UserId = groomer.UserId,
            DisplayName = groomer.DisplayName,
            Active = groomer.Active,
            Capabilities = groomer.Capabilities.Select(c => new GroomerCapabilityResponse
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
            WorkingSchedules = groomer.WorkingSchedules.Select(s => new WorkingScheduleResponse
            {
                Id = s.Id,
                GroomerId = s.GroomerId,
                Weekday = s.Weekday,
                StartLocalTime = s.StartLocalTime,
                EndLocalTime = s.EndLocalTime,
                CreatedAtUtc = s.CreatedAtUtc,
                UpdatedAtUtc = s.UpdatedAtUtc
            }).ToArray(),
            CreatedAtUtc = groomer.CreatedAtUtc,
            UpdatedAtUtc = groomer.UpdatedAtUtc
        }, cancellation: ct);
    }
}

public sealed class GetGroomerByIdRequest
{
    public Guid GroomerId { get; set; }
}
