using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Staff.Api.Admin.ListGroomers;

public sealed class ListGroomersEndpoint(IStaffReadService staffReadService)
    : EndpointWithoutRequest<ListGroomersResponse>
{
    public override void Configure()
    {
        Get("/api/admin/groomers");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await staffReadService.ListGroomersAsync(ct);
        await Send.ResponseAsync(new ListGroomersResponse
        {
            Items = items.Select(x => new GroomerListItemResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                DisplayName = x.DisplayName,
                Active = x.Active,
                CapabilityCount = x.CapabilityCount,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToArray()
        }, cancellation: ct);
    }
}