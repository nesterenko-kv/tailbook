using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Staff.Api.Admin.ListGroomers;

public sealed class ListGroomersEndpoint(IStaffQueries staffQueries)
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
        var items = await staffQueries.ListGroomersAsync(ct);
        await Send.ResponseAsync(new ListGroomersResponse
        {
            Items = items.Select(x => new GroomerListItemResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                DisplayName = x.DisplayName,
                Active = x.Active,
                CapabilityCount = x.CapabilityCount,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            }).ToArray()
        }, cancellation: ct);
    }
}

public sealed class ListGroomersResponse
{
    public GroomerListItemResponse[] Items { get; set; } = [];
}

public sealed class GroomerListItemResponse
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public int CapabilityCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
