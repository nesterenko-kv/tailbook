using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Audit.Application.AuditEntries.Queries;

namespace Tailbook.Modules.Audit.Api.Admin.ListAuditEntries;

public sealed class ListAuditEntriesEndpoint(IAuditEntryReadService auditEntryReadService)
    : Endpoint<ListAuditEntriesRequest, ListAuditEntriesResponse>
{
    public override void Configure()
    {
        Get("/api/admin/audit");
        Description(x => x.WithTags("Audit"));
        PermissionsAll("audit.trail.read");
    }

    public override async Task HandleAsync(ListAuditEntriesRequest req, CancellationToken ct)
    {
        var result = await auditEntryReadService.ListAsync(new ListAuditEntriesQuery(req.ModuleCode, req.EntityType, req.EntityId, req.Page, req.PageSize), ct);
        await Send.OkAsync(result.ToResponse(), ct);
    }
}
